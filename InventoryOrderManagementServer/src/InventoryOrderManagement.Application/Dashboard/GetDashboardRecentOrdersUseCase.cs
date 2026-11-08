using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Application.Dashboard.Dtos;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Dashboard;

public sealed class GetDashboardRecentOrdersUseCase
{
    private const int DefaultTake = 10;

    private readonly IDashboardQuery _dashboardQuery;

    public GetDashboardRecentOrdersUseCase(IDashboardQuery dashboardQuery)
    {
        _dashboardQuery = dashboardQuery;
    }

    public async Task<DashboardRecentOrdersResponse> ExecuteAsync(
        int? take,
        CancellationToken cancellationToken = default)
    {
        var limit = take ?? DefaultTake;
        if (limit is < 1 or > 50)
        {
            throw InvalidReportRequestException.InvalidLimit();
        }

        var rows = await _dashboardQuery.GetRecentOrdersAsync(limit, cancellationToken);
        return new DashboardRecentOrdersResponse
        {
            Items = rows
                .Select(row => new DashboardRecentOrderResponse
                {
                    OrderId = row.OrderId,
                    OrderNumber = row.OrderNumber,
                    CustomerName = row.CustomerName,
                    Status = row.Status,
                    TotalAmount = row.Subtotal,
                    CreatedAtUtc = row.CreatedAtUtc,
                    ConfirmedAtUtc = row.ConfirmedAtUtc
                })
                .ToList()
        };
    }
}
