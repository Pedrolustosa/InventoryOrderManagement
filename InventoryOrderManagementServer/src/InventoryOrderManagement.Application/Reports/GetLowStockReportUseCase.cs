using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetLowStockReportUseCase
{
    private readonly IInventoryReportsQuery _inventoryReportsQuery;

    public GetLowStockReportUseCase(IInventoryReportsQuery inventoryReportsQuery)
    {
        _inventoryReportsQuery = inventoryReportsQuery;
    }

    public async Task<PagedResult<LowStockReportResponse>> ExecuteAsync(
        Guid? categoryId,
        bool? isActive,
        LowStockSort? sortBy,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var result = await _inventoryReportsQuery.GetLowStockAsync(
            new LowStockReportFilter
            {
                CategoryId = categoryId,
                IsActive = isActive,
                SortBy = sortBy ?? LowStockSort.Quantity,
                Page = resolvedPage,
                PageSize = resolvedPageSize
            },
            cancellationToken);

        var items = result.Items
            .Select(row => new LowStockReportResponse
            {
                ProductId = row.ProductId,
                Sku = row.Sku,
                ProductName = row.ProductName,
                CategoryName = row.CategoryName,
                QuantityOnHand = row.QuantityOnHand,
                MinimumStockLevel = row.MinimumStockLevel,
                StockDifference = row.MinimumStockLevel - row.QuantityOnHand
            })
            .ToList();

        return PagedResult<LowStockReportResponse>.Create(items, result.Page, result.PageSize, result.TotalItems);
    }
}
