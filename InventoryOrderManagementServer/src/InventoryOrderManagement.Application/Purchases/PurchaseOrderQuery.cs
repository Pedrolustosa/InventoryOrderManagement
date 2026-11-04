using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class PurchaseOrderQuery
{
    public PurchaseOrderStatus? Status { get; init; }

    public Guid? SupplierId { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public string? PurchaseOrderNumber { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
