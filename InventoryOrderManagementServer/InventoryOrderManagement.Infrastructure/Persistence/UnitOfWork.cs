using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence;

/// <summary>
/// Wraps one SQLite transaction around use-case work.
/// A concurrency conflict is translated here so Application does not reference EF Core.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext;

    public UnitOfWork(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InventoryConcurrencyException(exception);
        }
    }
}
