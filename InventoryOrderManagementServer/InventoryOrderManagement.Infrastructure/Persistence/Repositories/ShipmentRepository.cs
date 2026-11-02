using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly InventoryDbContext _dbContext;

    public ShipmentRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Shipments.AddAsync(shipment, cancellationToken);
    }

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .AsNoTracking()
            .Include(entity => entity.History)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<Shipment?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .Include(entity => entity.History)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .AsNoTracking()
            .Include(entity => entity.History)
            .FirstOrDefaultAsync(entity => entity.OrderId == orderId, cancellationToken);
    }

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .AsNoTracking()
            .AnyAsync(entity => entity.OrderId == orderId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Shipment> Items, int TotalItems)> GetPageAsync(
        ShipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var shipments = _dbContext.Shipments
            .AsNoTracking()
            .AsQueryable();

        if (query.Status.HasValue)
        {
            shipments = shipments.Where(entity => entity.Status == query.Status.Value);
        }

        if (query.OrderId.HasValue)
        {
            shipments = shipments.Where(entity => entity.OrderId == query.OrderId.Value);
        }

        if (query.CustomerId.HasValue)
        {
            shipments = shipments.Where(entity => entity.CustomerId == query.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CarrierName))
        {
            var carrier = query.CarrierName.Trim();
            shipments = shipments.Where(entity => entity.CarrierName == carrier);
        }

        if (!string.IsNullOrWhiteSpace(query.TrackingCode))
        {
            var tracking = query.TrackingCode.Trim();
            shipments = shipments.Where(entity => entity.TrackingCode == tracking);
        }

        if (query.StartDateUtc.HasValue)
        {
            shipments = shipments.Where(entity => entity.CreatedAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            shipments = shipments.Where(entity => entity.CreatedAtUtc <= query.EndDateUtc.Value);
        }

        var totalItems = await shipments.CountAsync(cancellationToken);
        var items = await shipments
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ThenByDescending(entity => entity.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }
}
