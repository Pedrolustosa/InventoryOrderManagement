using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Abstractions.Reports;

public interface IInventoryReportsQuery
{
    Task<InventoryStockCounts> GetStockCountsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryBalanceRow>> GetInventoryAsync(
        InventoryReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryBalanceRow>> GetLowStockAsync(
        LowStockReportFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryStockCounts
{
    public int TotalProducts { get; init; }

    public int ActiveProducts { get; init; }

    public int InactiveProducts { get; init; }

    public int TotalUnitsInStock { get; init; }

    public int LowStockProducts { get; init; }

    public int OutOfStockProducts { get; init; }
}

public sealed class InventoryBalanceRow
{
    public Guid ProductId { get; init; }

    public string Sku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public string CategoryName { get; init; } = null!;

    public int QuantityOnHand { get; init; }

    public int MinimumStockLevel { get; init; }

    public bool IsActive { get; init; }
}

public sealed class InventoryReportFilter
{
    public Guid? ProductId { get; init; }

    public Guid? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsLowStock { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}

public sealed class LowStockReportFilter
{
    public Guid? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public LowStockSort SortBy { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}
