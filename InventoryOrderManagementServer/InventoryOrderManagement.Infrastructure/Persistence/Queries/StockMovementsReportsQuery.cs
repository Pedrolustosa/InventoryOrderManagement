using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

public sealed class StockMovementsReportsQuery : IStockMovementsReportsQuery
{
    private readonly InventoryDbContext _dbContext;

    public StockMovementsReportsQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<StockMovementReportRow>> GetPageAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = Apply(Source(), filter)
            .OrderByDescending(row => row.CreatedAtUtc)
            .ThenBy(row => row.MovementId);

        var totalItems = await rows.CountAsync(cancellationToken);
        var items = await rows
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(row => new StockMovementReportRow
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
            })
            .ToListAsync(cancellationToken);

        return PagedResult<StockMovementReportRow>.Create(items, filter.Page, filter.PageSize, totalItems);
    }

    public async Task<IReadOnlyList<MovementTypeQuantity>> GetQuantitiesByTypeAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        return await Apply(Source(), filter)
            .GroupBy(row => row.MovementType)
            .Select(group => new MovementTypeQuantity
            {
                MovementType = group.Key,
                Quantity = group.Sum(row => row.Quantity)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovementInstantQuantity>> GetQuantitiesAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        return await Apply(Source(), filter)
            .Select(row => new MovementInstantQuantity
            {
                CreatedAtUtc = row.CreatedAtUtc,
                Quantity = row.Quantity
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<MovementRead> Source()
    {
        return
            from movement in _dbContext.StockMovements.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on movement.ProductId equals product.Id
            join supplier in _dbContext.Suppliers.AsNoTracking()
                on movement.SupplierId equals supplier.Id into suppliers
            from supplier in suppliers.DefaultIfEmpty()
            select new MovementRead
            {
                MovementId = movement.Id,
                ProductId = product.Id,
                ProductSku = product.Sku,
                ProductName = product.Name,
                SupplierId = movement.SupplierId,
                SupplierName = supplier != null ? supplier.Name : null,
                OrderId = movement.OrderId,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                PreviousQuantity = movement.PreviousQuantity,
                NewQuantity = movement.NewQuantity,
                Reason = movement.Reason,
                CreatedAtUtc = movement.CreatedAtUtc
            };
    }

    private static IQueryable<MovementRead> Apply(IQueryable<MovementRead> rows, StockMovementReportFilter filter)
    {
        if (filter.ProductId.HasValue)
        {
            rows = rows.Where(row => row.ProductId == filter.ProductId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            rows = rows.Where(row => row.SupplierId == filter.SupplierId.Value);
        }

        if (filter.OrderId.HasValue)
        {
            rows = rows.Where(row => row.OrderId == filter.OrderId.Value);
        }

        if (filter.MovementType.HasValue)
        {
            rows = rows.Where(row => row.MovementType == filter.MovementType.Value);
        }

        if (filter.StartUtc.HasValue)
        {
            rows = rows.Where(row => row.CreatedAtUtc >= filter.StartUtc.Value);
        }

        if (filter.EndUtc.HasValue)
        {
            rows = rows.Where(row => row.CreatedAtUtc < filter.EndUtc.Value);
        }

        return rows;
    }

    private sealed class MovementRead
    {
        public Guid MovementId { get; set; }

        public Guid ProductId { get; set; }

        public string ProductSku { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public Guid? SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public Guid? OrderId { get; set; }

        public StockMovementType MovementType { get; set; }

        public int Quantity { get; set; }

        public int PreviousQuantity { get; set; }

        public int NewQuantity { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
