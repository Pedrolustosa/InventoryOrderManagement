using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class CashMovementRepository : ICashMovementRepository
{
    private readonly InventoryDbContext _dbContext;

    public CashMovementRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CashMovement movement, CancellationToken cancellationToken = default)
    {
        await _dbContext.CashMovements.AddAsync(movement, cancellationToken);
    }

    public async Task<IReadOnlyList<CashMovement>> ListAsync(
        CashMovementListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var movements = _dbContext.CashMovements.AsNoTracking().AsQueryable();

        if (filter.CashAccountId.HasValue)
        {
            movements = movements.Where(movement => movement.CashAccountId == filter.CashAccountId.Value);
        }

        if (filter.StartUtc.HasValue)
        {
            movements = movements.Where(movement => movement.OccurredAtUtc >= filter.StartUtc.Value);
        }

        if (filter.EndUtc.HasValue)
        {
            movements = movements.Where(movement => movement.OccurredAtUtc < filter.EndUtc.Value);
        }

        return await movements
            .OrderBy(movement => movement.OccurredAtUtc)
            .ThenBy(movement => movement.Id)
            .ToListAsync(cancellationToken);
    }
}
