using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task DispatchTransactionalAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(typeof(ITransactionalDomainEventHandler<>), domainEvents, cancellationToken);

    public Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(typeof(IDomainEventHandler<>), domainEvents, cancellationToken);

    private async Task DispatchCoreAsync(
        Type openHandlerType,
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = openHandlerType.MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handleMethod = handlerType.GetMethod(
                    "HandleAsync",
                    [domainEvent.GetType(), typeof(CancellationToken)]);

                if (handleMethod is null)
                {
                    throw new InvalidOperationException(
                        $"HandleAsync was not found on handler type '{handlerType}'.");
                }

                var task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
                if (task is null)
                {
                    throw new InvalidOperationException(
                        $"HandleAsync on '{handler.GetType()}' returned null.");
                }

                await task.ConfigureAwait(false);
            }
        }
    }
}
