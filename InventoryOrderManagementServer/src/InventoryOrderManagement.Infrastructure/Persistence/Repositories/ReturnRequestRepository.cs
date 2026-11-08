using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly InventoryDbContext _dbContext;

    public ReturnRequestRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.ReturnRequests.AddAsync(returnRequest, cancellationToken);
    }

    public Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ReturnRequests
            .AsNoTracking()
            .Include(entity => entity.Items)
            .Include(entity => entity.ReplacementItems)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<ReturnRequest?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ReturnRequests
            .Include(entity => entity.Items)
            .Include(entity => entity.ReplacementItems)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNumberAsync(string returnNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.ReturnRequests
            .AsNoTracking()
            .AnyAsync(entity => entity.ReturnNumber == returnNumber, cancellationToken);
    }

    public async Task<(IReadOnlyList<ReturnRequest> Items, int TotalItems)> GetPageAsync(
        ReturnRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returns = _dbContext.ReturnRequests
            .AsNoTracking()
            .Include(entity => entity.Items)
            .Include(entity => entity.ReplacementItems)
            .AsSplitQuery()
            .AsQueryable();

        if (query.Type.HasValue)
        {
            returns = returns.Where(entity => entity.Type == query.Type.Value);
        }

        if (query.Status.HasValue)
        {
            returns = returns.Where(entity => entity.Status == query.Status.Value);
        }

        if (query.CustomerId.HasValue)
        {
            returns = returns.Where(entity => entity.CustomerId == query.CustomerId.Value);
        }

        if (query.SupplierId.HasValue)
        {
            returns = returns.Where(entity => entity.SupplierId == query.SupplierId.Value);
        }

        if (query.OrderId.HasValue)
        {
            returns = returns.Where(entity => entity.OrderId == query.OrderId.Value);
        }

        if (query.PurchaseOrderId.HasValue)
        {
            returns = returns.Where(entity => entity.PurchaseOrderId == query.PurchaseOrderId.Value);
        }

        if (query.StartDateUtc.HasValue)
        {
            returns = returns.Where(entity => entity.CreatedAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            returns = returns.Where(entity => entity.CreatedAtUtc <= query.EndDateUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ReturnNumber))
        {
            returns = returns.Where(entity => entity.ReturnNumber == query.ReturnNumber);
        }

        var totalItems = await returns.CountAsync(cancellationToken);
        var items = await returns
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ThenByDescending(entity => entity.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }
}
