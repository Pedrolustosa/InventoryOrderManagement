namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class OrderTrendPointResponse
{
    public string Period { get; init; } = null!;

    public int TotalOrders { get; init; }

    public decimal TotalConfirmedOrderValue { get; init; }
}

public sealed class OrderTrendReportResponse
{
    public required IReadOnlyList<OrderTrendPointResponse> Periods { get; init; }
}
