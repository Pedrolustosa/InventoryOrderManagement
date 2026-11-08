using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Domain.UnitTests.Abstractions;

public class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_AccumulatesEvents()
    {
        var aggregate = new TestAggregate();
        var first = new TestDomainEvent(DateTime.UtcNow.AddMinutes(-1));
        var second = new TestDomainEvent(DateTime.UtcNow);

        aggregate.Publish(first);
        aggregate.Publish(second);

        Assert.Equal(2, aggregate.DomainEvents.Count);
        Assert.Equal([first, second], aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.Publish(new TestDomainEvent(DateTime.UtcNow));
        aggregate.Publish(new TestDomainEvent(DateTime.UtcNow));

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_IsReadOnly()
    {
        var aggregate = new TestAggregate();
        aggregate.Publish(new TestDomainEvent(DateTime.UtcNow));

        Assert.IsAssignableFrom<IReadOnlyCollection<IDomainEvent>>(aggregate.DomainEvents);
        Assert.Throws<NotSupportedException>(() => ((ICollection<IDomainEvent>)aggregate.DomainEvents).Clear());
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void Publish(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(DateTime OccurredOnUtc) : IDomainEvent;
}
