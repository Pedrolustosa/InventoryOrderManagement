using System.Reflection;
using System.Runtime.ExceptionServices;
using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryOrderManagement.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? NullLogger<DomainEventDispatcher>.Instance;
    }

    /// <summary>Runs inside the transaction: a failing handler propagates and rolls everything back.</summary>
    public Task DispatchTransactionalAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(typeof(ITransactionalDomainEventHandler<>), domainEvents, isolateFailures: false, cancellationToken);

    /// <summary>
    /// Runs after commit: the operation is already persisted, so each handler is isolated and a failure is
    /// only logged. There is no outbox, so the side effect of a failed handler is lost.
    /// </summary>
    public Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(typeof(IDomainEventHandler<>), domainEvents, isolateFailures: true, cancellationToken);

    private async Task DispatchCoreAsync(
        Type openHandlerType,
        IEnumerable<IDomainEvent> domainEvents,
        bool isolateFailures,
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

                try
                {
                    await InvokeAsync(handlerType, handler, domainEvent, cancellationToken);
                }
                catch (Exception exception) when (isolateFailures && exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Post-commit handler {Handler} failed for domain event {DomainEvent}.",
                        handler.GetType().Name,
                        domainEvent.GetType().Name);
                }
            }
        }
    }

    private static async Task InvokeAsync(
        Type handlerType,
        object handler,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var handleMethod = handlerType.GetMethod(
            "HandleAsync",
            [domainEvent.GetType(), typeof(CancellationToken)]);

        if (handleMethod is null)
        {
            throw new InvalidOperationException(
                $"HandleAsync was not found on handler type '{handlerType}'.");
        }

        Task? task;
        try
        {
            task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            // A handler that throws synchronously must surface its own exception (e.g. a 409 conflict),
            // not the reflection wrapper.
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        if (task is null)
        {
            throw new InvalidOperationException(
                $"HandleAsync on '{handler.GetType()}' returned null.");
        }

        await task.ConfigureAwait(false);
    }
}
