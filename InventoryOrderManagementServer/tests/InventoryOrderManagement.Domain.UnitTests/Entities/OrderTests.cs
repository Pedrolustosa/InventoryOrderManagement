using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class OrderTests
{
    [Fact]
    public void Confirm_FromDraft_RaisesOrderConfirmedDomainEventOnce()
    {
        var order = CreateDraftOrder();

        order.Confirm();

        var domainEvent = Assert.Single(order.DomainEvents);
        var confirmed = Assert.IsType<OrderConfirmedDomainEvent>(domainEvent);
        Assert.Equal(order.Id, confirmed.OrderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Single(confirmed.Items);
        Assert.Equal(2, confirmed.Items[0].Quantity);
    }

    [Fact]
    public void Confirm_FromConfirmed_ThrowsAndDoesNotRaiseEvent()
    {
        var order = CreateDraftOrder();
        order.Confirm();
        order.ClearDomainEvents();

        Assert.Throws<InvalidOrderStatusException>(() => order.Confirm());
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Ship_DoesNotRaiseOrderConfirmedDomainEvent()
    {
        var order = CreateDraftOrder();
        order.Confirm();
        order.ClearDomainEvents();

        order.Ship();

        Assert.Empty(order.DomainEvents);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    private static Order CreateDraftOrder()
    {
        var productId = Guid.NewGuid();
        return Order.Create(
            "ORD-TEST-1",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(productId, "SKU-1", "Widget", 2, 10m)]);
    }
}
