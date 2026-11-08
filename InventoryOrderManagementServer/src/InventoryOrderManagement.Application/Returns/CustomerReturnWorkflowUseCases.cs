using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Returns;

public sealed class ReceiveCustomerReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<ReceiveCustomerReturnUseCase> _logger;

    public ReceiveCustomerReturnUseCase(
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser,
        INotificationDispatcher notificationDispatcher,
        ILogger<ReceiveCustomerReturnUseCase> logger)
    {
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        ReceiveCustomerReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.Receive(
                request.Items.Select(item => (item.ReturnItemId, item.ReceivedQuantity)).ToList(),
                request.Notes);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.CustomerReturnReceived,
            updated.Id,
            cancellationToken);

        await NotificationDispatch.TryAsync(
            ct => _notificationDispatcher.NotifyReturnAwaitingInspectionAsync(
                updated.Id,
                updated.ReturnNumber,
                updated.CustomerName ?? "Customer",
                updated.ReceivedAtUtc ?? DateTime.UtcNow,
                ct),
            _logger,
            $"Failed to dispatch return-awaiting-inspection notification for return {updated.Id}.",
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class StartReturnInspectionUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public StartReturnInspectionUseCase(
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.StartInspection();
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ReturnInspectionStarted,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class CompleteCustomerReturnInspectionUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<CompleteCustomerReturnInspectionUseCase> _logger;

    public CompleteCustomerReturnInspectionUseCase(
        IReturnRequestRepository returnRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser,
        INotificationDispatcher notificationDispatcher,
        ILogger<CompleteCustomerReturnInspectionUseCase> logger)
    {
        _returnRepository = returnRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        CompleteReturnInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Stock, order return quantities and finance are applied by ReturnCompletedHandler in this same
        // transaction when the inspection completes the return.
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            if (!entity.IsCustomerSide)
            {
                throw new DomainException("This operation applies only to customer returns.");
            }

            var lines = request.Items
                .Select(item => new InspectionLine(
                    item.ReturnItemId,
                    item.Condition,
                    item.RestockedQuantity,
                    item.QuarantinedQuantity,
                    item.DiscardedQuantity,
                    item.RejectedQuantity,
                    item.InspectionNotes))
                .ToList();

            entity.CompleteInspection(lines, request.Notes, _currentUser.UserId);
        }, cancellationToken);

        var lowStockChanges = await ReturnLowStockChanges.CollectAsync(
            _inventoryRepository,
            returnRequestId,
            cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ReturnInspectionCompleted,
            updated.Id,
            cancellationToken,
            details: updated.Status.ToString());

        await NotificationDispatch.TryNotifyLowStockAsync(
            _notificationDispatcher,
            _logger,
            lowStockChanges,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}
