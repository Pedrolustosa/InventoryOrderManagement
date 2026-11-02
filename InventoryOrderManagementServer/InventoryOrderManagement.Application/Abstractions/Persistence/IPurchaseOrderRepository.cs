using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IPurchaseOrderRepository
{
    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchaseOrder> Items, int TotalItems)> GetPageAsync(
        PurchaseOrderQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default);

    void StageItemReplacement(
        IReadOnlyCollection<PurchaseOrderItem> removedItems,
        IReadOnlyCollection<PurchaseOrderItem> currentItems);
}

public interface IPurchaseReceiptRepository
{
    Task AddAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default);

    Task<PurchaseReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseReceipt>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(string receiptNumber, CancellationToken cancellationToken = default);
}
