namespace InventoryOrderManagement.Application.Inventory;

public sealed class InventoryBalanceDetails
{
    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public required bool ProductIsActive { get; init; }

    public required int QuantityOnHand { get; init; }

    public required int MinimumStockLevel { get; init; }

    public bool IsLowStock => QuantityOnHand <= MinimumStockLevel;
}
