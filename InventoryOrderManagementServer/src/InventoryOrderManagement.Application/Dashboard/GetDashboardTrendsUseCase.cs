using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Application.Dashboard.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Reports;

namespace InventoryOrderManagement.Application.Dashboard;

public sealed class GetDashboardTrendsUseCase
{
    private readonly IDashboardQuery _dashboardQuery;
    private readonly TimeProvider _time;

    public GetDashboardTrendsUseCase(IDashboardQuery dashboardQuery, TimeProvider time)
    {
        _dashboardQuery = dashboardQuery;
        _time = time;
    }

    public async Task<DashboardTrendsResponse> ExecuteAsync(
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

        var window = ReportWindow.Resolve(
            startDateUtc,
            endDateUtc,
            period,
            _time.GetUtcNow().UtcDateTime);
        window.EnsureBounds();

        var sales = await _dashboardQuery.GetSalesTrendPointsAsync(
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var purchases = await _dashboardQuery.GetPurchaseTrendPointsAsync(
            window.StartUtc,
            window.EndUtc,
            cancellationToken);

        var salesGrouped = sales
            .GroupBy(point => ReportBuckets.Align(point.ConfirmedAtUtc, groupBy.Value))
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Amount: group.Sum(point => point.Subtotal)));

        var purchasesGrouped = purchases
            .GroupBy(point => ReportBuckets.Align(point.SentAtUtc, groupBy.Value))
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Amount: group.Sum(point => point.Subtotal)));

        var items = ReportBuckets.Enumerate(window.StartUtc!.Value, window.EndUtc!.Value, groupBy.Value)
            .Select(key =>
            {
                salesGrouped.TryGetValue(key, out var salesBucket);
                purchasesGrouped.TryGetValue(key, out var purchaseBucket);
                return new DashboardTrendItemResponse
                {
                    Period = ReportBuckets.Key(key),
                    SalesCount = salesBucket.Count,
                    SalesAmount = MoneyMath.Round(salesBucket.Amount),
                    PurchasesCount = purchaseBucket.Count,
                    PurchasesAmount = MoneyMath.Round(purchaseBucket.Amount)
                };
            })
            .ToList();

        return new DashboardTrendsResponse
        {
            GroupBy = groupBy.Value.ToString(),
            Items = items
        };
    }
}
