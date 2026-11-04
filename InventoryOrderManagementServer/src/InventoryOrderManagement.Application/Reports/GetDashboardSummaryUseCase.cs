using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetDashboardSummaryUseCase
{
    private readonly IInventoryReportsQuery _inventoryReportsQuery;
    private readonly IOrderReportsQuery _orderReportsQuery;

    public GetDashboardSummaryUseCase(
        IInventoryReportsQuery inventoryReportsQuery,
        IOrderReportsQuery orderReportsQuery)
    {
        _inventoryReportsQuery = inventoryReportsQuery;
        _orderReportsQuery = orderReportsQuery;
    }

    public async Task<DashboardSummaryResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        var stock = await _inventoryReportsQuery.GetStockCountsAsync(cancellationToken);
        var counts = await _orderReportsQuery.GetStatusCountsAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var commercial = await _orderReportsQuery.GetCommercialOrdersAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var (total, average, confirmedCount) = CommercialValue.Summarize(commercial);

        return new DashboardSummaryResponse
        {
            TotalProducts = stock.TotalProducts,
            ActiveProducts = stock.ActiveProducts,
            InactiveProducts = stock.InactiveProducts,
            TotalUnitsInStock = stock.TotalUnitsInStock,
            LowStockProducts = stock.LowStockProducts,
            OutOfStockProducts = stock.OutOfStockProducts,
            TotalOrdersCreated = counts.TotalOrdersCreated,
            DraftOrders = counts.DraftOrders,
            ConfirmedOrders = counts.ConfirmedOrders,
            ShippedOrders = counts.ShippedOrders,
            DeliveredOrders = counts.DeliveredOrders,
            CancelledOrders = counts.CancelledOrders,
            TotalConfirmedOrderValue = total,
            AverageConfirmedOrderValue = average,
            TotalConfirmedOrders = confirmedCount,
            TotalCancelledOrders = counts.CancelledOrders
        };
    }
}
