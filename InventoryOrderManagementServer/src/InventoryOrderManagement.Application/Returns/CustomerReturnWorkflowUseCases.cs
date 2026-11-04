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
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<CompleteCustomerReturnInspectionUseCase> _logger;

    public CompleteCustomerReturnInspectionUseCase(
        IReturnRequestRepository returnRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser,
        INotificationDispatcher notificationDispatcher,
        ILogger<CompleteCustomerReturnInspectionUseCase> logger)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
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

        var lowStockChanges = new List<LowStockChange>();

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            if (entity.Type != ReturnType.CustomerReturn)
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

            var acceptedLines = entity.Items
                .Where(item => item.AcceptedQuantity > 0)
                .Select(item => (item.OrderItemId!.Value, item.AcceptedQuantity))
                .ToList();

            if (acceptedLines.Count > 0)
            {
                var order = await _orderRepository.GetTrackedByIdAsync(entity.OrderId!.Value, ct)
                    ?? throw new OrderNotFoundException(entity.OrderId!.Value);

                order.RegisterCustomerReturn(acceptedLines);
            }

            foreach (var item in entity.Items.Where(item => item.RestockedQuantity > 0).OrderBy(item => item.ProductId))
            {
                var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, ct)
                    ?? throw new InventoryBalanceNotFoundException(item.ProductId);

                var previous = balance.QuantityOnHand;
                balance.Receive(item.RestockedQuantity);
                var movement = StockMovement.CreateForCustomerReturn(
                    item.ProductId,
                    entity.OrderId!.Value,
                    entity.Id,
                    item.Id,
                    item.RestockedQuantity,
                    previous,
                    balance.QuantityOnHand,
                    $"Customer return {entity.ReturnNumber}");

                await _inventoryRepository.AddMovementAsync(movement, ct);
                lowStockChanges.Add(new LowStockChange(
                    item.ProductId,
                    item.ProductName,
                    item.ProductSku,
                    previous,
                    balance.QuantityOnHand,
                    balance.MinimumStockLevel,
                    movement.Id));
            }
        }, cancellationToken);

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
