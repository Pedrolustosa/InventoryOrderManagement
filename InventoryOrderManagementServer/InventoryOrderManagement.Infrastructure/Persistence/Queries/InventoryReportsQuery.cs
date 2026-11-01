using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

public sealed class InventoryReportsQuery : IInventoryReportsQuery
{
    private readonly InventoryDbContext _dbContext;

    public InventoryReportsQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryStockCounts> GetStockCountsAsync(CancellationToken cancellationToken = default)
    {
        var products = _dbContext.Products.AsNoTracking();
        var balances = _dbContext.InventoryBalances.AsNoTracking();

        return new InventoryStockCounts
        {
            TotalProducts = await products.CountAsync(cancellationToken),
            ActiveProducts = await products.CountAsync(product => product.IsActive, cancellationToken),
            InactiveProducts = await products.CountAsync(product => !product.IsActive, cancellationToken),
            TotalUnitsInStock = await balances.SumAsync(balance => (int?)balance.QuantityOnHand, cancellationToken) ?? 0,
            LowStockProducts = await balances.CountAsync(
                balance => balance.QuantityOnHand <= balance.MinimumStockLevel,
                cancellationToken),
            OutOfStockProducts = await balances.CountAsync(
                balance => balance.QuantityOnHand == 0,
                cancellationToken)
        };
    }

    public async Task<PagedResult<InventoryBalanceRow>> GetInventoryAsync(
        InventoryReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = Source();

        if (filter.ProductId.HasValue)
        {
            rows = rows.Where(row => row.ProductId == filter.ProductId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            rows = rows.Where(row => row.CategoryId == filter.CategoryId.Value);
        }

        if (filter.IsActive.HasValue)
        {
            rows = rows.Where(row => row.IsActive == filter.IsActive.Value);
        }

        if (filter.IsLowStock == true)
        {
            rows = rows.Where(row => row.QuantityOnHand <= row.MinimumStockLevel);
        }
        else if (filter.IsLowStock == false)
        {
            rows = rows.Where(row => row.QuantityOnHand > row.MinimumStockLevel);
        }

        rows = rows.OrderBy(row => row.ProductName).ThenBy(row => row.Sku);

        return await PageAsync(rows, filter.Page, filter.PageSize, cancellationToken);
    }

    public async Task<PagedResult<InventoryBalanceRow>> GetLowStockAsync(
        LowStockReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = Source().Where(row => row.QuantityOnHand <= row.MinimumStockLevel);

        if (filter.CategoryId.HasValue)
        {
            rows = rows.Where(row => row.CategoryId == filter.CategoryId.Value);
        }

        if (filter.IsActive.HasValue)
        {
            rows = rows.Where(row => row.IsActive == filter.IsActive.Value);
        }

        rows = filter.SortBy switch
        {
            LowStockSort.Difference => rows
                .OrderByDescending(row => row.MinimumStockLevel - row.QuantityOnHand)
                .ThenBy(row => row.ProductName),
            LowStockSort.Name => rows.OrderBy(row => row.ProductName).ThenBy(row => row.Sku),
            LowStockSort.Sku => rows.OrderBy(row => row.Sku),
            _ => rows.OrderBy(row => row.QuantityOnHand).ThenBy(row => row.ProductName)
        };

        return await PageAsync(rows, filter.Page, filter.PageSize, cancellationToken);
    }

    private IQueryable<InventoryBalanceRead> Source()
    {
        return
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on balance.ProductId equals product.Id
            join category in _dbContext.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            select new InventoryBalanceRead
            {
                ProductId = product.Id,
                Sku = product.Sku,
                ProductName = product.Name,
                CategoryId = product.CategoryId,
                CategoryName = category.Name,
                QuantityOnHand = balance.QuantityOnHand,
                MinimumStockLevel = balance.MinimumStockLevel,
                IsActive = product.IsActive
            };
    }

    private static async Task<PagedResult<InventoryBalanceRow>> PageAsync(
        IQueryable<InventoryBalanceRead> rows,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalItems = await rows.CountAsync(cancellationToken);
        var items = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new InventoryBalanceRow
            {
                ProductId = row.ProductId,
                Sku = row.Sku,
                ProductName = row.ProductName,
                CategoryName = row.CategoryName,
                QuantityOnHand = row.QuantityOnHand,
                MinimumStockLevel = row.MinimumStockLevel,
                IsActive = row.IsActive
            })
            .ToListAsync(cancellationToken);

        return PagedResult<InventoryBalanceRow>.Create(items, page, pageSize, totalItems);
    }

    private sealed class InventoryBalanceRead
    {
        public Guid ProductId { get; set; }

        public string Sku { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        public int QuantityOnHand { get; set; }

        public int MinimumStockLevel { get; set; }

        public bool IsActive { get; set; }
    }
}
