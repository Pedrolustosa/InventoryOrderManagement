using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Fulfillment;

/// <summary>
/// The only way to ship or deliver a sales order: the order follows its shipment, in the same transaction.
/// Order.MarkShipped/MarkDelivered are internal, so nothing outside the domain can move an order to
/// Shipped or Delivered without a shipment of that order in the matching status.
/// </summary>
public static class OrderFulfillment
{
    public static void Dispatch(Order order, Shipment shipment, string? notes, Guid? changedByUserId)
    {
        EnsureSameOrder(order, shipment);
        EnsureOrderStatus(order, OrderStatus.Confirmed);

        var items = order.Items
            .Select(item => new ShipmentDispatchedItem(item.ProductId, item.Quantity))
            .ToList();

        shipment.Dispatch(notes, changedByUserId, items);
        order.MarkShipped(shipment);
    }

    public static void Deliver(Order order, Shipment shipment, string? notes, Guid? changedByUserId)
    {
        EnsureSameOrder(order, shipment);
        EnsureOrderStatus(order, OrderStatus.Shipped);

        shipment.MarkAsDelivered(notes, changedByUserId);
        order.MarkDelivered(shipment);
    }

    // Checked before the shipment changes, so a rejected call leaves both aggregates untouched.
    private static void EnsureSameOrder(Order order, Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipment);

        if (shipment.OrderId != order.Id)
        {
            throw new ShipmentConflictException("The shipment does not belong to this order.");
        }
    }

    private static void EnsureOrderStatus(Order order, OrderStatus expected)
    {
        if (order.Status != expected)
        {
            throw new InvalidOrderStatusException(order.Status);
        }
    }
}
