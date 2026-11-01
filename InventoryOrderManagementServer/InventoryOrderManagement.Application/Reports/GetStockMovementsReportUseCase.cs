using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetStockMovementsReportUseCase
{
    private readonly IStockMovementsReportsQuery _stockMovementsReportsQuery;

    public GetStockMovementsReportUseCase(IStockMovementsReportsQuery stockMovementsReportsQuery)
    {
        _stockMovementsReportsQuery = stockMovementsReportsQuery;
    }

    public async Task<StockMovementReportResponse> ExecuteAsync(
        Guid? productId,
        Guid? supplierId,
        Guid? orderId,
        StockMovementType? movementType,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        ReportGroupBy? groupBy,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, DateTime.UtcNow);
        if (groupBy.HasValue)
        {
            window.EnsureBounds();
        }

        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var filter = new StockMovementReportFilter
        {
            ProductId = productId,
            SupplierId = supplierId,
            OrderId = orderId,
            MovementType = movementType,
            StartUtc = window.StartUtc,
            EndUtc = window.EndUtc,
            Page = resolvedPage,
            PageSize = resolvedPageSize
        };

        var pageResult = await _stockMovementsReportsQuery.GetPageAsync(filter, cancellationToken);
        var quantities = await _stockMovementsReportsQuery.GetQuantitiesByTypeAsync(filter, cancellationToken);
        IReadOnlyList<StockMovementPeriodResponse>? periods = null;

        if (groupBy.HasValue)
        {
            var points = await _stockMovementsReportsQuery.GetQuantitiesAsync(filter, cancellationToken);
            periods = Bucket(points, window, groupBy.Value);
        }

        return new StockMovementReportResponse
        {
            Movements = PagedResult<StockMovementReportItemResponse>.Create(
                pageResult.Items.Select(Map).ToList(),
                pageResult.Page,
                pageResult.PageSize,
                pageResult.TotalItems),
            Totals = Totals(quantities),
            Periods = periods
        };
    }

    private static StockMovementReportItemResponse Map(StockMovementReportRow row)
    {
        return new StockMovementReportItemResponse
        {
            MovementId = row.MovementId,
            ProductId = row.ProductId,
            ProductSku = row.ProductSku,
            ProductName = row.ProductName,
            SupplierId = row.SupplierId,
            SupplierName = row.SupplierName,
            OrderId = row.OrderId,
            MovementType = row.MovementType,
            Quantity = row.Quantity,
            PreviousQuantity = row.PreviousQuantity,
            NewQuantity = row.NewQuantity,
            Reason = row.Reason,
            CreatedAtUtc = row.CreatedAtUtc
        };
    }

    private static StockMovementTotalsResponse Totals(IReadOnlyList<MovementTypeQuantity> quantities)
    {
        int Quantity(StockMovementType type)
        {
            return quantities.Where(row => row.MovementType == type).Sum(row => row.Quantity);
        }

        return new StockMovementTotalsResponse
        {
            TotalEntries = Quantity(StockMovementType.Entry),
            TotalExit = Quantity(StockMovementType.Exit),
            TotalAdjustmentIncrease = Quantity(StockMovementType.AdjustmentIncrease),
            TotalAdjustmentDecrease = Quantity(StockMovementType.AdjustmentDecrease),
            TotalOrderExit = Quantity(StockMovementType.OrderExit),
            TotalOrderCancellationReturn = Quantity(StockMovementType.OrderCancellationReturn)
        };
    }

    private static IReadOnlyList<StockMovementPeriodResponse> Bucket(
        IReadOnlyList<MovementInstantQuantity> points,
        ReportWindow window,
        ReportGroupBy groupBy)
    {
        var grouped = points
            .GroupBy(point => ReportBuckets.Align(point.CreatedAtUtc, groupBy))
            .ToDictionary(group => group.Key, group => group.Sum(point => point.Quantity));

        return ReportBuckets.Enumerate(window.StartUtc!.Value, window.EndUtc!.Value, groupBy)
            .Select(key => new StockMovementPeriodResponse
            {
                Period = ReportBuckets.Key(key),
                TotalQuantity = grouped.TryGetValue(key, out var quantity) ? quantity : 0
            })
            .ToList();
    }
}
