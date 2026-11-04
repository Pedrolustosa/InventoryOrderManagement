namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class OrderSummaryReportResponse
{
    public int TotalOrders { get; init; }

    public int DraftOrders { get; init; }

    public int ConfirmedOrders { get; init; }

    public int ShippedOrders { get; init; }

    public int DeliveredOrders { get; init; }

    public int CancelledOrders { get; init; }

    public int TotalCancelledOrders { get; init; }

    public decimal TotalConfirmedOrderValue { get; init; }

    public decimal AverageConfirmedOrderValue { get; init; }
}
