using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Application.UnitTests.Events;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_InvokesAllHandlersForMatchingEventTypeOnly()
    {
        var eventAHandler1 = new RecordingHandler<EventA>();
        var eventAHandler2 = new RecordingHandler<EventA>();
        var eventBHandler = new RecordingHandler<EventB>();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<EventA>>(eventAHandler1);
        services.AddSingleton<IDomainEventHandler<EventA>>(eventAHandler2);
        services.AddSingleton<IDomainEventHandler<EventB>>(eventBHandler);

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        var eventA = new EventA(DateTime.UtcNow);

        await dispatcher.DispatchAsync([eventA]);

        Assert.Equal(1, eventAHandler1.CallCount);
        Assert.Equal(1, eventAHandler2.CallCount);
        Assert.Equal(0, eventBHandler.CallCount);
        Assert.Same(eventA, eventAHandler1.LastEvent);
        Assert.Same(eventA, eventAHandler2.LastEvent);
    }

    [Fact]
    public async Task DispatchAsync_AfterCommit_IsolatesAFailingHandler()
    {
        var after = new RecordingHandler<EventA>();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<EventA>>(new ThrowingHandler<EventA>());
        services.AddSingleton<IDomainEventHandler<EventA>>(after);
        await using var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        var exception = await Record.ExceptionAsync(() => dispatcher.DispatchAsync([new EventA(DateTime.UtcNow)]));

        Assert.Null(exception);
        Assert.Equal(1, after.CallCount);
    }

    [Fact]
    public async Task DispatchTransactionalAsync_PropagatesAFailingHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransactionalDomainEventHandler<EventA>>(new ThrowingTransactionalHandler<EventA>());
        await using var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchTransactionalAsync([new EventA(DateTime.UtcNow)]));
    }

    private sealed class ThrowingHandler<TEvent> : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Handler failed.");
    }

    private sealed class ThrowingTransactionalHandler<TEvent> : ITransactionalDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Handler failed.");
    }

    private sealed record EventA(DateTime OccurredOnUtc) : IDomainEvent;

    private sealed record EventB(DateTime OccurredOnUtc) : IDomainEvent;

    private sealed class RecordingHandler<TEvent> : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public int CallCount { get; private set; }

        public TEvent? LastEvent { get; private set; }

        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastEvent = domainEvent;
            return Task.CompletedTask;
        }
    }
}
