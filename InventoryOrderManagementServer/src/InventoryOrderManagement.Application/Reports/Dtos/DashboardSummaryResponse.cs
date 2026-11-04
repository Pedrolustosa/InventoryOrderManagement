namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class DashboardSummaryResponse
{
    public int TotalProducts { get; init; }

    public int ActiveProducts { get; init; }

    public int InactiveProducts { get; init; }

    public int TotalUnitsInStock { get; init; }

    public int LowStockProducts { get; init; }

    public int OutOfStockProducts { get; init; }

    public int TotalOrdersCreated { get; init; }

    public int DraftOrders { get; init; }

    public int ConfirmedOrders { get; init; }

    public int ShippedOrders { get; init; }

    public int DeliveredOrders { get; init; }

    public int CancelledOrders { get; init; }

    public decimal TotalConfirmedOrderValue { get; init; }

    public decimal AverageConfirmedOrderValue { get; init; }

    public int TotalConfirmedOrders { get; init; }

    public int TotalCancelledOrders { get; init; }
}
