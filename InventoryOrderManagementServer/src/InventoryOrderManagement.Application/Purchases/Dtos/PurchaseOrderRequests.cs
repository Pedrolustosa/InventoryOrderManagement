namespace InventoryOrderManagement.Application.Purchases.Dtos;

public sealed class PurchaseOrderItemRequest
{
    public Guid ProductId { get; init; }

    public int OrderedQuantity { get; init; }

    public decimal UnitCost { get; init; }
}

public sealed class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<PurchaseOrderItemRequest>? Items { get; init; }
}

public sealed class UpdatePurchaseOrderRequest
{
    public string? Notes { get; init; }

    public IReadOnlyList<PurchaseOrderItemRequest>? Items { get; init; }
}

public sealed class CancelPurchaseOrderRequest
{
    public string? Reason { get; init; }
}

public sealed class ReceivePurchaseOrderItemRequest
{
    public Guid PurchaseOrderItemId { get; init; }

    public int QuantityReceived { get; init; }
}

public sealed class ReceivePurchaseOrderRequest
{
    public string? Notes { get; init; }

    public IReadOnlyList<ReceivePurchaseOrderItemRequest>? Items { get; init; }
}
