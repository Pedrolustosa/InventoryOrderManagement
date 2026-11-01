using InventoryOrderManagement.Application.Reports;

namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class InventoryReportResponse
{
    public Guid ProductId { get; init; }

    public string Sku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public string CategoryName { get; init; } = null!;

    public int QuantityOnHand { get; init; }

    public int MinimumStockLevel { get; init; }

    public bool IsActive { get; init; }

    public bool IsLowStock { get; init; }

    public StockReportStatus StockStatus { get; init; }
}
