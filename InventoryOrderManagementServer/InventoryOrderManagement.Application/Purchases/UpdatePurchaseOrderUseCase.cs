using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class UpdatePurchaseOrderUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseDraftBuilder _draftBuilder;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public UpdatePurchaseOrderUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _draftBuilder = new PurchaseDraftBuilder(supplierRepository, productRepository);
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<PurchaseOrderResponse> ExecuteAsync(
        Guid purchaseOrderId,
        UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _purchaseOrderRepository.GetTrackedByIdAsync(purchaseOrderId, ct)
                ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

            var removedItems = order.Items.ToList();
            var items = await _draftBuilder.BuildItemsAsync(request.Items, ct);
            order.ReplaceDraft(request.Notes, items);
            _purchaseOrderRepository.StageItemReplacement(removedItems, order.Items);
        }, cancellationToken);

        var updated = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        await PurchaseAudit.WriteAsync(_auditWriter, _currentUser, SecurityActions.PurchaseOrderUpdated, updated.Id, cancellationToken);
        return PurchaseOrderResponse.FromOrder(updated);
    }
}
