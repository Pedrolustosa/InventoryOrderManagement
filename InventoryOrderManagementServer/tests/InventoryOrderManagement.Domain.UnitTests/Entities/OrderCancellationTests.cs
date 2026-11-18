using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Fulfillment;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class OrderCancellationTests
{
    [Fact]
    public void ReleaseReservation_ReducesReservedOnly_AndBumpsTheVersion()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(10);
        balance.Reserve(6);
        var version = balance.Version;

        balance.ReleaseReservation(4);

        Assert.Equal(10, balance.QuantityOnHand);
        Assert.Equal(2, balance.ReservedQuantity);
        Assert.Equal(8, balance.AvailableQuantity);
        Assert.Equal(version + 1, balance.Version);
    }

    [Fact]
    public void ReleaseReservation_MoreThanReserved_ThrowsAndChangesNothing()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(10);
        balance.Reserve(2);

        Assert.Throws<DomainException>(() => balance.ReleaseReservation(3));
        Assert.Equal(2, balance.ReservedQuantity);
        Assert.Equal(10, balance.QuantityOnHand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseReservation_NonPositive_Throws(int quantity)
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(10);
        balance.Reserve(2);

        Assert.Throws<DomainException>(() => balance.ReleaseReservation(quantity));
        Assert.Equal(2, balance.ReservedQuantity);
    }

    [Fact]
    public void ReleaseReservation_BackAboveTheMinimum_DoesNotNotifyAndRearmsTheAlert()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(10);
        balance.SetMinimumStockLevel(5);
        balance.Reserve(6);
        balance.ClearDomainEvents();

        balance.ReleaseReservation(6);

        Assert.Empty(balance.DomainEvents);
        Assert.False(balance.IsLowStock);

        balance.Reserve(6);
        Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(balance.DomainEvents));
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Confirmed)]
    public void Cancel_FromDraftOrConfirmed_RaisesTheEventOnceWithThePreviousStatus(OrderStatus status)
    {
        var order = OrderIn(status);

        order.Cancel("Customer changed plans");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        var raised = Assert.IsType<OrderCancelledDomainEvent>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, raised.OrderId);
        Assert.Equal(status, raised.PreviousStatus);
        var item = Assert.Single(raised.Items);
        Assert.Equal(order.Items.Single().ProductId, item.ProductId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(order.CancelledAtUtc, raised.OccurredOnUtc);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public void Cancel_AfterShipping_IsBlockedAndRaisesNothing(OrderStatus status)
    {
        var order = OrderIn(status);

        Assert.Throws<InvalidOrderStatusException>(() => order.Cancel("Too late"));
        Assert.Equal(status, order.Status);
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Cancel_Twice_IsBlockedAndDoesNotRaiseAgain()
    {
        var order = OrderIn(OrderStatus.Confirmed);
        order.Cancel("First");
        order.ClearDomainEvents();

        Assert.Throws<InvalidOrderStatusException>(() => order.Cancel("Second"));
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Shipment_Cancel_OnlyFromPreparing_AndRecordsHistory()
    {
        var shipment = Shipment.Create(Guid.NewGuid(), "ORD-1", Guid.NewGuid(), "Jane", "Acme", null, null, null);

        shipment.Cancel("Order cancelled", null);

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        var last = shipment.History.OrderBy(entry => entry.OccurredAtUtc).Last();
        Assert.Equal(ShipmentStatus.Preparing, last.PreviousStatus);
        Assert.Equal(ShipmentStatus.Cancelled, last.NewStatus);
        Assert.Throws<InvalidShipmentStatusException>(() => shipment.Cancel(null, null));

        var dispatched = Shipment.Create(Guid.NewGuid(), "ORD-2", Guid.NewGuid(), "Jane", "Acme", null, null, null);
        dispatched.Dispatch(null, null, []);
        Assert.Throws<InvalidShipmentStatusException>(() => dispatched.Cancel(null, null));
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = Order.Create(
            "ORD-CANCEL",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", 3, 10m)]);

        if (status is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered)
        {
            order.Confirm();
        }

        var shipment = Shipment.Create(order.Id, order.OrderNumber, order.CustomerId, order.CustomerName, "Acme", null, null, null);
        if (status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            OrderFulfillment.Dispatch(order, shipment, null, null);
        }

        if (status == OrderStatus.Delivered)
        {
            OrderFulfillment.Deliver(order, shipment, null, null);
        }

        order.ClearDomainEvents();
        return order;
    }
}
