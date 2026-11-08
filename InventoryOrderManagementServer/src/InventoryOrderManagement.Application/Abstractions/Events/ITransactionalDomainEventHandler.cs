using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Application.Abstractions.Events;

public interface ITransactionalDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
