using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.ValueObjects;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Orders;

public class OrderConfirmedHandlerTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();

    [Fact]
    public async Task HandleAsync_WithSufficientStock_ReservesAllItems()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var order = CreateOrder(
            [
                OrderItem.Create(productA, "SKU-A", "Product A", 2, 10m),
                OrderItem.Create(productB, "SKU-B", "Product B", 3, 5m)
            ]);
        order.Confirm();
        order.ClearDomainEvents();

        var balanceA = InventoryBalance.Create(productA);
        balanceA.Receive(10);
        var balanceB = InventoryBalance.Create(productB);
        balanceB.Receive(10);

        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetTrackedByProductIdAsync(productA, Arg.Any<CancellationToken>()).Returns(balanceA);
        _inventory.GetTrackedByProductIdAsync(productB, Arg.Any<CancellationToken>()).Returns(balanceB);

        var handler = new OrderConfirmedHandler(_orders, _inventory);
        var domainEvent = new OrderConfirmedDomainEvent(
            order.Id,
            [
                new OrderConfirmedItem(productA, 2),
                new OrderConfirmedItem(productB, 3)
            ],
            DateTime.UtcNow);

        await handler.HandleAsync(domainEvent);

        Assert.Equal(2, balanceA.ReservedQuantity);
        Assert.Equal(3, balanceB.ReservedQuantity);
        await _inventory.Received(2).AddMovementAsync(
            Arg.Is<StockMovement>(movement => movement.MovementType == StockMovementType.OrderReservation),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSecondItemLacksStock_DoesNotLeavePartialReservations()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var order = CreateOrder(
            [
                OrderItem.Create(productA, "SKU-A", "Product A", 2, 10m),
                OrderItem.Create(productB, "SKU-B", "Product B", 5, 5m)
            ]);
        order.Confirm();
        order.ClearDomainEvents();

        var balanceA = InventoryBalance.Create(productA);
        balanceA.Receive(10);
        var balanceB = InventoryBalance.Create(productB);
        balanceB.Receive(1);

        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetTrackedByProductIdAsync(productA, Arg.Any<CancellationToken>()).Returns(balanceA);
        _inventory.GetTrackedByProductIdAsync(productB, Arg.Any<CancellationToken>()).Returns(balanceB);

        var handler = new OrderConfirmedHandler(_orders, _inventory);
        var domainEvent = new OrderConfirmedDomainEvent(
            order.Id,
            [
                new OrderConfirmedItem(productA, 2),
                new OrderConfirmedItem(productB, 5)
            ],
            DateTime.UtcNow);

        var exception = await Assert.ThrowsAsync<OrderInsufficientStockException>(
            () => handler.HandleAsync(domainEvent));

        Assert.Equal("Product B", exception.ProductName);
        Assert.Equal(0, balanceA.ReservedQuantity);
        Assert.Equal(0, balanceB.ReservedQuantity);
        await _inventory.DidNotReceive().AddMovementAsync(
            Arg.Any<StockMovement>(),
            Arg.Any<CancellationToken>());
    }

    private static Order CreateOrder(IReadOnlyList<OrderItem> items) =>
        Order.Create(
            "ORD-HANDLER-1",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            items);
}
