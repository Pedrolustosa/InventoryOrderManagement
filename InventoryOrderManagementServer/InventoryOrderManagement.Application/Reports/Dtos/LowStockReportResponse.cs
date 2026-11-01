namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class LowStockReportResponse
{
    public Guid ProductId { get; init; }

    public string Sku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public string CategoryName { get; init; } = null!;

    public int QuantityOnHand { get; init; }

    public int MinimumStockLevel { get; init; }

    public int StockDifference { get; init; }
}
