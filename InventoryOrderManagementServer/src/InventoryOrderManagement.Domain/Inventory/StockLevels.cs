namespace InventoryOrderManagement.Domain.Inventory;

/// <summary>
/// The single low-stock rule: a product is below its minimum when a minimum is configured (greater than
/// zero) and the available quantity (on hand minus reserved) is lower than it.
/// </summary>
public static class StockLevels
{
    public static bool IsBelowMinimum(int availableQuantity, int minimumStockLevel) =>
        minimumStockLevel > 0 && availableQuantity < minimumStockLevel;
}
