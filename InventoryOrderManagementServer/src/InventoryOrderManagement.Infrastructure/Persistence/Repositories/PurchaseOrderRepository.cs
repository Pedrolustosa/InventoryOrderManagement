using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly InventoryDbContext _dbContext;

    public PurchaseOrderRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        await _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
    }

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public Task<PurchaseOrder?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int TotalItems)> GetPageAsync(
        PurchaseOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orders = _dbContext.PurchaseOrders.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            orders = orders.Where(order => order.Status == query.Status.Value);
        }

        if (query.SupplierId.HasValue)
        {
            orders = orders.Where(order => order.SupplierId == query.SupplierId.Value);
        }

        if (query.StartDateUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc <= query.EndDateUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PurchaseOrderNumber))
        {
            orders = orders.Where(order => order.PurchaseOrderNumber == query.PurchaseOrderNumber);
        }

        var totalItems = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<bool> ExistsByNumberAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseOrders
            .AsNoTracking()
            .AnyAsync(order => order.PurchaseOrderNumber == purchaseOrderNumber, cancellationToken);
    }

    public void StageItemReplacement(
        IReadOnlyCollection<PurchaseOrderItem> removedItems,
        IReadOnlyCollection<PurchaseOrderItem> currentItems)
    {
        foreach (var item in removedItems)
        {
            var entry = _dbContext.Entry(item);
            if (entry.State != EntityState.Deleted)
            {
                entry.State = EntityState.Deleted;
            }
        }

        foreach (var item in currentItems)
        {
            var entry = _dbContext.Entry(item);
            if (entry.State != EntityState.Added)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
