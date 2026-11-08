using InventoryOrderManagement.Domain.Inventory;

namespace InventoryOrderManagement.Application.Reports;

internal static class StockStatusClassifier
{
    /// <summary>
    /// Out of stock when nothing is physically on hand; low stock when the available quantity (on hand minus
    /// reserved) is below a configured minimum (<see cref="StockLevels"/>).
    /// </summary>
    public static StockReportStatus Classify(int quantityOnHand, int reservedQuantity, int minimumStockLevel)
    {
        if (quantityOnHand == 0)
        {
            return StockReportStatus.OutOfStock;
        }

        if (IsLowStock(quantityOnHand, reservedQuantity, minimumStockLevel))
        {
            return StockReportStatus.LowStock;
        }

        return StockReportStatus.InStock;
    }

    public static bool IsLowStock(int quantityOnHand, int reservedQuantity, int minimumStockLevel) =>
        StockLevels.IsBelowMinimum(quantityOnHand - reservedQuantity, minimumStockLevel);
}
