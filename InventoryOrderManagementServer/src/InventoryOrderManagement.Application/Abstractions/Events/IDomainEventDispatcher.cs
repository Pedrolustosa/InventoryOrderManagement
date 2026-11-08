using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchTransactionalAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);

    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
