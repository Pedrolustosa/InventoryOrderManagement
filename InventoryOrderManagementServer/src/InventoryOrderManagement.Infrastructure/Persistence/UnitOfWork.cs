using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UnitOfWork(
        InventoryDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _dbContext = dbContext;
        _domainEventDispatcher = domainEventDispatcher;
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

            var domainEvents = CollectDomainEvents();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (domainEvents.Count > 0)
            {
                await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InventoryConcurrencyException(exception);
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity)
            .OfType<AggregateRoot>()
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return domainEvents;
    }
}
