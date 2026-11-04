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

public sealed class AuthorizeSupplierReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public AuthorizeSupplierReturnUseCase(
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
        AuthorizeSupplierReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.Authorize(request.Notes, request.AuthorizationReference);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.SupplierReturnAuthorized,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class DispatchSupplierReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<DispatchSupplierReturnUseCase> _logger;

    public DispatchSupplierReturnUseCase(
        IReturnRequestRepository returnRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser,
        INotificationDispatcher notificationDispatcher,
        ILogger<DispatchSupplierReturnUseCase> logger)
    {
        _returnRepository = returnRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        DispatchSupplierReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lowStockChanges = new List<LowStockChange>();

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            if (entity.Type != ReturnType.SupplierReturn)
            {
                throw new DomainException("This operation applies only to supplier returns.");
            }

            var purchaseOrder = await _purchaseOrderRepository.GetTrackedByIdAsync(entity.PurchaseOrderId!.Value, ct)
                ?? throw new PurchaseOrderNotFoundException(entity.PurchaseOrderId!.Value);

            var returnLines = entity.Items
                .Select(item => (item.PurchaseOrderItemId!.Value, item.RequestedQuantity))
                .ToList();

            purchaseOrder.RegisterSupplierReturn(returnLines);

            foreach (var item in entity.Items.OrderBy(item => item.ProductId))
            {
                var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, ct)
                    ?? throw new InventoryBalanceNotFoundException(item.ProductId);

                var previous = balance.QuantityOnHand;
                balance.Issue(item.RequestedQuantity);
                var movement = StockMovement.CreateForSupplierReturn(
                    item.ProductId,
                    entity.SupplierId!.Value,
                    entity.PurchaseOrderId!.Value,
                    entity.Id,
                    item.Id,
                    item.RequestedQuantity,
                    previous,
                    balance.QuantityOnHand,
                    $"Supplier return {entity.ReturnNumber}");

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

            entity.Dispatch(request.Notes, request.DispatchReference);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.SupplierReturnDispatched,
            updated.Id,
            cancellationToken);

        await NotificationDispatch.TryNotifyLowStockAsync(
            _notificationDispatcher,
            _logger,
            lowStockChanges,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class CancelReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CancelReturnUseCase(
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
        CancelReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.Cancel(request.Reason);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ReturnCancelled,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}
