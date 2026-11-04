using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetSalesByCustomerReportUseCase
{
    private readonly ISalesReportsQuery _salesReportsQuery;

    public GetSalesByCustomerReportUseCase(ISalesReportsQuery salesReportsQuery)
    {
        _salesReportsQuery = salesReportsQuery;
    }

    public async Task<IReadOnlyList<SalesByCustomerResponse>> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        var resolvedLimit = ReportLimit.Normalize(limit);
        var sales = await _salesReportsQuery.GetCustomerSalesAsync(window.StartUtc, window.EndUtc, cancellationToken);

        return sales
            .GroupBy(sale => sale.CustomerId)
            .Select(group =>
            {
                decimal subtotal = 0;
                var items = 0;
                foreach (var sale in group)
                {
                    subtotal += sale.Subtotal;
                    items += sale.ItemQuantity;
                }

                var latest = group.OrderByDescending(sale => sale.ConfirmedAtUtc).First();
                return new SalesByCustomerResponse
                {
                    CustomerId = group.Key,
                    CustomerName = latest.CustomerName,
                    TotalOrders = group.Count(),
                    TotalItemsSold = items,
                    TotalConfirmedOrderValue = MoneyMath.Round(subtotal),
                    AverageOrderValue = MoneyMath.Average(subtotal, group.Count())
                };
            })
            .OrderByDescending(customer => customer.TotalConfirmedOrderValue)
            .ThenBy(customer => customer.CustomerName)
            .Take(resolvedLimit)
            .ToList();
    }
}
