using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetOrderTrendReportUseCase
{
    private readonly IOrderReportsQuery _orderReportsQuery;

    public GetOrderTrendReportUseCase(IOrderReportsQuery orderReportsQuery)
    {
        _orderReportsQuery = orderReportsQuery;
    }

    public async Task<OrderTrendReportResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        ReportGroupBy? groupBy,
        CancellationToken cancellationToken = default)
    {
        if (groupBy is null)
        {
            throw InvalidReportRequestException.GroupByRequired();
        }

        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        window.EnsureBounds();

        var commercial = await _orderReportsQuery.GetCommercialOrdersAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);

        var grouped = commercial
            .GroupBy(order => ReportBuckets.Align(order.ConfirmedAtUtc, groupBy.Value))
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Value: group.Sum(order => order.Subtotal)));

        var periods = ReportBuckets.Enumerate(window.StartUtc!.Value, window.EndUtc!.Value, groupBy.Value)
            .Select(key =>
            {
                grouped.TryGetValue(key, out var value);
                return new OrderTrendPointResponse
                {
                    Period = ReportBuckets.Key(key),
                    TotalOrders = value.Count,
                    TotalConfirmedOrderValue = MoneyMath.Round(value.Value)
                };
            })
            .ToList();

        return new OrderTrendReportResponse
        {
            Periods = periods
        };
    }
}
