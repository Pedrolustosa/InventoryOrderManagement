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

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handleMethod = handlerType.GetMethod(
                    nameof(IDomainEventHandler<IDomainEvent>.HandleAsync),
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
