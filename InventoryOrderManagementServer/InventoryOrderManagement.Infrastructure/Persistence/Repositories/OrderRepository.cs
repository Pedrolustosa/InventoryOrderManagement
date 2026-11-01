using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly InventoryDbContext _dbContext;

    public OrderRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(order).State == EntityState.Detached)
        {
            _dbContext.Orders.Update(order);
        }

        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public Task<Order?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        OrderQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orders = _dbContext.Orders.AsNoTracking().AsQueryable();

        if (query.CustomerId.HasValue)
        {
            orders = orders.Where(order => order.CustomerId == query.CustomerId.Value);
        }

        if (query.Status.HasValue)
        {
            orders = orders.Where(order => order.Status == query.Status.Value);
        }

        if (query.StartDateUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc <= query.EndDateUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
        {
            orders = orders.Where(order => order.OrderNumber == query.OrderNumber);
        }

        return await orders
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .AnyAsync(order => order.OrderNumber == orderNumber, cancellationToken);
    }

    public void StageItemReplacement(
        IReadOnlyCollection<OrderItem> removedItems,
        IReadOnlyCollection<OrderItem> currentItems)
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
