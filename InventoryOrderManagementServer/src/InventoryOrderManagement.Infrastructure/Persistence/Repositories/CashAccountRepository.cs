using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class CashAccountRepository : ICashAccountRepository
{
    private readonly InventoryDbContext _dbContext;

    public CashAccountRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CashAccount account, CancellationToken cancellationToken = default)
    {
        await _dbContext.CashAccounts.AddAsync(account, cancellationToken);
    }

    public Task<CashAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CashAccounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public Task<CashAccount?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CashAccounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CashAccount>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CashAccounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasMovementsAsync(Guid cashAccountId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CashMovements.AnyAsync(movement => movement.CashAccountId == cashAccountId, cancellationToken);
    }
}
