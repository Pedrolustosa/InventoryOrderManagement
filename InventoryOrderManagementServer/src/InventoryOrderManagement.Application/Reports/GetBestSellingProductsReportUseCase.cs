using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetBestSellingProductsReportUseCase
{
    private readonly ISalesReportsQuery _salesReportsQuery;

    public GetBestSellingProductsReportUseCase(ISalesReportsQuery salesReportsQuery)
    {
        _salesReportsQuery = salesReportsQuery;
    }

    public async Task<IReadOnlyList<BestSellingProductResponse>> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        var resolvedLimit = ReportLimit.Normalize(limit);
        var items = await _salesReportsQuery.GetSoldItemsAsync(window.StartUtc, window.EndUtc, cancellationToken);

        return items
            .GroupBy(item => item.ProductId)
            .Select(group =>
            {
                decimal lineTotal = 0;
                var quantity = 0;
                foreach (var item in group)
                {
                    lineTotal += item.LineTotal;
                    quantity += item.Quantity;
                }

                var latest = group.OrderByDescending(item => item.ConfirmedAtUtc).First();
                return new BestSellingProductResponse
                {
                    ProductId = group.Key,
                    ProductSku = latest.ProductSku,
                    ProductName = latest.ProductName,
                    TotalQuantitySold = quantity,
                    TotalConfirmedOrderValue = MoneyMath.Round(lineTotal),
                    TotalOrders = group.Select(item => item.OrderId).Distinct().Count()
                };
            })
            .OrderByDescending(item => item.TotalQuantitySold)
            .ThenByDescending(item => item.TotalConfirmedOrderValue)
            .ThenBy(item => item.ProductName)
            .Take(resolvedLimit)
            .ToList();
    }
}
