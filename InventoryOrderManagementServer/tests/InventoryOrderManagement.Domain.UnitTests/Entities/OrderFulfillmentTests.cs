using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Fulfillment;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class OrderFulfillmentTests
{
    [Fact]
    public void Order_ExposesNoPublicWayToShipOrDeliver()
    {
        var publicTransitions = typeof(Order).GetMethods()
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Where(name => name.Contains("Ship", StringComparison.Ordinal) || name.Contains("Deliver", StringComparison.Ordinal));

        Assert.Empty(publicTransitions);
    }

    [Fact]
    public void Dispatch_ShipsTheOrderWithItsShipment_AndRaisesTheDispatchWithTheOrderItems()
    {
        var order = ConfirmedOrder();
        var shipment = ShipmentOf(order);

        OrderFulfillment.Dispatch(order, shipment, "Left the warehouse", null);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Equal(shipment.ShippedAtUtc, order.ShippedAtUtc);
        var dispatched = Assert.IsType<ShipmentDispatchedDomainEvent>(Assert.Single(shipment.DomainEvents));
        var item = Assert.Single(dispatched.Items);
        Assert.Equal(order.Items.Single().ProductId, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void Deliver_DeliversTheOrderWithItsShipment()
    {
        var order = ConfirmedOrder();
        var shipment = ShipmentOf(order);
        OrderFulfillment.Dispatch(order, shipment, null, null);
        shipment.SetInTransit(null, null);

        OrderFulfillment.Deliver(order, shipment, "Signed", null);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(shipment.DeliveredAtUtc, order.DeliveredAtUtc);
    }

    [Fact]
    public void Dispatch_WithAShipmentOfAnotherOrder_IsRejectedAndChangesNothing()
    {
        var order = ConfirmedOrder();
        var otherShipment = ShipmentOf(ConfirmedOrder());

        Assert.Throws<ShipmentConflictException>(() => OrderFulfillment.Dispatch(order, otherShipment, null, null));

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(ShipmentStatus.Preparing, otherShipment.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Cancelled)]
    public void Dispatch_OfAnOrderThatIsNotConfirmed_IsRejectedBeforeTheShipmentMoves(OrderStatus status)
    {
        var order = DraftOrder();
        if (status == OrderStatus.Cancelled)
        {
            order.Cancel("Not needed");
        }

        var shipment = ShipmentOf(order);

        Assert.Throws<InvalidOrderStatusException>(() => OrderFulfillment.Dispatch(order, shipment, null, null));

        Assert.Equal(status, order.Status);
        Assert.Equal(ShipmentStatus.Preparing, shipment.Status);
    }

    [Fact]
    public void Deliver_BeforeTheOrderShipped_IsRejectedAndChangesNothing()
    {
        var order = ConfirmedOrder();
        var shipment = ShipmentOf(order);

        Assert.Throws<InvalidOrderStatusException>(() => OrderFulfillment.Deliver(order, shipment, null, null));

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(ShipmentStatus.Preparing, shipment.Status);
    }

    [Fact]
    public void Deliver_WithAShipmentOfAnotherOrder_IsRejected()
    {
        var order = ConfirmedOrder();
        OrderFulfillment.Dispatch(order, ShipmentOf(order), null, null);
        var other = ConfirmedOrder();
        var otherShipment = ShipmentOf(other);
        OrderFulfillment.Dispatch(other, otherShipment, null, null);

        Assert.Throws<ShipmentConflictException>(() => OrderFulfillment.Deliver(order, otherShipment, null, null));

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(ShipmentStatus.Shipped, otherShipment.Status);
    }

    [Fact]
    public void Dispatch_WithoutACarrier_LeavesTheOrderConfirmed()
    {
        var order = ConfirmedOrder();
        var shipment = Shipment.Create(order.Id, order.OrderNumber, order.CustomerId, order.CustomerName, null, null, null, null);

        Assert.Throws<DomainException>(() => OrderFulfillment.Dispatch(order, shipment, null, null));

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Null(order.ShippedAtUtc);
    }

    private static Order DraftOrder() => Order.Create(
        "ORD-FUL",
        Guid.NewGuid(),
        "Jane Doe",
        OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
        [OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", 2, 10m)]);

    private static Order ConfirmedOrder()
    {
        var order = DraftOrder();
        order.Confirm();
        order.ClearDomainEvents();
        return order;
    }

    private static Shipment ShipmentOf(Order order) =>
        Shipment.Create(order.Id, order.OrderNumber, order.CustomerId, order.CustomerName, "Acme", null, null, null);
}
