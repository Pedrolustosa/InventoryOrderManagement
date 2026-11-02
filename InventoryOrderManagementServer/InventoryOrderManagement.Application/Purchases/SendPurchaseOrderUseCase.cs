using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class SendPurchaseOrderUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public SendPurchaseOrderUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<PurchaseOrderResponse> ExecuteAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _purchaseOrderRepository.GetTrackedByIdAsync(purchaseOrderId, ct)
                ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

            var supplier = await _supplierRepository.GetByIdAsync(order.SupplierId, ct)
                ?? throw new SupplierNotFoundException(order.SupplierId);

            if (!supplier.IsActive)
            {
                throw new InactiveSupplierPurchaseException(supplier.Id);
            }

            order.Send();
        }, cancellationToken);

        var sent = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        await PurchaseAudit.WriteAsync(_auditWriter, _currentUser, SecurityActions.PurchaseOrderSent, sent.Id, cancellationToken);
        return PurchaseOrderResponse.FromOrder(sent);
    }
}
