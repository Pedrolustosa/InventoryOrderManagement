using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetCancellationReportUseCase
{
    private readonly IOrderReportsQuery _orderReportsQuery;

    public GetCancellationReportUseCase(IOrderReportsQuery orderReportsQuery)
    {
        _orderReportsQuery = orderReportsQuery;
    }

    public async Task<CancellationReportResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        var cohort = await _orderReportsQuery.GetCancellationCohortAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var cancelled = await _orderReportsQuery.GetCancelledOrdersAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);

        decimal cancellationValue = 0;
        foreach (var order in cancelled)
        {
            cancellationValue += order.Subtotal;
        }

        return new CancellationReportResponse
        {
            OrdersCreated = cohort.OrdersCreated,
            TotalCancelledOrders = cohort.CreatedAndCancelled,
            CancellationRate = MoneyMath.Rate(cohort.CreatedAndCancelled, cohort.OrdersCreated),
            CancelledInPeriod = cancelled.Count,
            CancellationValue = MoneyMath.Round(cancellationValue),
            CancellationByPeriod = Bucket(cancelled, window)
        };
    }

    private static IReadOnlyList<CancellationPeriodResponse> Bucket(
        IReadOnlyList<CancelledOrderAmount> cancelled,
        ReportWindow window)
    {
        var grouped = cancelled
            .GroupBy(order => ReportBuckets.Align(order.CancelledAtUtc, ReportGroupBy.Day))
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Value: group.Sum(order => order.Subtotal)));

        if (!window.HasBounds)
        {
            return grouped
                .OrderBy(pair => pair.Key)
                .Select(pair => new CancellationPeriodResponse
                {
                    Period = ReportBuckets.Key(pair.Key),
                    TotalCancelledOrders = pair.Value.Count,
                    CancellationValue = MoneyMath.Round(pair.Value.Value)
                })
                .ToList();
        }

        return ReportBuckets.Enumerate(window.StartUtc!.Value, window.EndUtc!.Value, ReportGroupBy.Day)
            .Select(key =>
            {
                grouped.TryGetValue(key, out var value);
                return new CancellationPeriodResponse
                {
                    Period = ReportBuckets.Key(key),
                    TotalCancelledOrders = value.Count,
                    CancellationValue = MoneyMath.Round(value.Value)
                };
            })
            .ToList();
    }
}
