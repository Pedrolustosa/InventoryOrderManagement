using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Reports;

public sealed class GetInventoryReportUseCase
{
    private readonly IInventoryReportsQuery _inventoryReportsQuery;

    public GetInventoryReportUseCase(IInventoryReportsQuery inventoryReportsQuery)
    {
        _inventoryReportsQuery = inventoryReportsQuery;
    }

    public async Task<PagedResult<InventoryReportResponse>> ExecuteAsync(
        Guid? productId,
        Guid? categoryId,
        bool? isActive,
        bool? isLowStock,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var result = await _inventoryReportsQuery.GetInventoryAsync(
            new InventoryReportFilter
            {
                ProductId = productId,
                CategoryId = categoryId,
                IsActive = isActive,
                IsLowStock = isLowStock,
                Page = resolvedPage,
                PageSize = resolvedPageSize
            },
            cancellationToken);

        var items = result.Items
            .Select(row => new InventoryReportResponse
            {
                ProductId = row.ProductId,
                Sku = row.Sku,
                ProductName = row.ProductName,
                CategoryName = row.CategoryName,
                QuantityOnHand = row.QuantityOnHand,
                MinimumStockLevel = row.MinimumStockLevel,
                IsActive = row.IsActive,
                IsLowStock = StockStatusClassifier.IsLowStock(row.QuantityOnHand, row.MinimumStockLevel),
                StockStatus = StockStatusClassifier.Classify(row.QuantityOnHand, row.MinimumStockLevel)
            })
            .ToList();

        return PagedResult<InventoryReportResponse>.Create(items, result.Page, result.PageSize, result.TotalItems);
    }
}
