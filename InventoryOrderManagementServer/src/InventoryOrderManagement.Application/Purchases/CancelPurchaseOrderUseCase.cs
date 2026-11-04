using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class CancelPurchaseOrderUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CancelPurchaseOrderUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<PurchaseOrderResponse> ExecuteAsync(
        Guid purchaseOrderId,
        CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _purchaseOrderRepository.GetTrackedByIdAsync(purchaseOrderId, ct)
                ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

            order.Cancel(request.Reason ?? string.Empty);
        }, cancellationToken);

        var cancelled = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        await PurchaseAudit.WriteAsync(_auditWriter, _currentUser, SecurityActions.PurchaseOrderCancelled, cancelled.Id, cancellationToken);
        return PurchaseOrderResponse.FromOrder(cancelled);
    }
}
