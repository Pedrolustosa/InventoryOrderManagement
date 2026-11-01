using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetOrderSummaryReportUseCase
{
    private readonly IOrderReportsQuery _orderReportsQuery;

    public GetOrderSummaryReportUseCase(IOrderReportsQuery orderReportsQuery)
    {
        _orderReportsQuery = orderReportsQuery;
    }

    public async Task<OrderSummaryReportResponse> ExecuteAsync(
        Guid? customerId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        var counts = await _orderReportsQuery.GetStatusCountsAsync(
            customerId,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var commercial = await _orderReportsQuery.GetCommercialOrdersAsync(
            customerId,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var (total, average, _) = CommercialValue.Summarize(commercial);

        return new OrderSummaryReportResponse
        {
            TotalOrders = counts.TotalOrdersCreated,
            DraftOrders = counts.DraftOrders,
            ConfirmedOrders = counts.ConfirmedOrders,
            ShippedOrders = counts.ShippedOrders,
            DeliveredOrders = counts.DeliveredOrders,
            CancelledOrders = counts.CancelledOrders,
            TotalCancelledOrders = counts.CancelledOrders,
            TotalConfirmedOrderValue = total,
            AverageConfirmedOrderValue = average
        };
    }
}
