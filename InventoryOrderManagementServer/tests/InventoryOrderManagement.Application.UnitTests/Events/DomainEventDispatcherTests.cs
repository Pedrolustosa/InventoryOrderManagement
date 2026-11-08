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
