namespace InventoryOrderManagement.Application.Reports;

internal static class StockStatusClassifier
{
    public static StockReportStatus Classify(int quantityOnHand, int minimumStockLevel)
    {
        if (quantityOnHand == 0)
        {
            return StockReportStatus.OutOfStock;
        }

        if (quantityOnHand <= minimumStockLevel)
        {
            return StockReportStatus.LowStock;
        }

        return StockReportStatus.InStock;
    }

    public static bool IsLowStock(int quantityOnHand, int minimumStockLevel)
    {
        return quantityOnHand <= minimumStockLevel;
    }
}
