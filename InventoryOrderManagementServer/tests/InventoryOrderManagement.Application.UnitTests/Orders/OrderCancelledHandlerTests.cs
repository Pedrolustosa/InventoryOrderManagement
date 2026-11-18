using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Orders;

public class OrderCancelledHandlerTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<OrderCancelledHandler> _logger = Substitute.For<ILogger<OrderCancelledHandler>>();
    private readonly List<StockMovement> _movements = [];
    private readonly Guid _productA = Guid.NewGuid();
    private readonly Guid _productB = Guid.NewGuid();
    private readonly InventoryBalance _balanceA;
    private readonly InventoryBalance _balanceB;

    public OrderCancelledHandlerTests()
    {
        _balanceA = InventoryBalance.Create(_productA);
        _balanceA.Receive(10);
        _balanceB = InventoryBalance.Create(_productB);
        _balanceB.Receive(10);
        _inventory.GetTrackedByProductIdAsync(_productA, Arg.Any<CancellationToken>()).Returns(_balanceA);
        _inventory.GetTrackedByProductIdAsync(_productB, Arg.Any<CancellationToken>()).Returns(_balanceB);
        _inventory.AddMovementAsync(Arg.Do<StockMovement>(_movements.Add), Arg.Any<CancellationToken>());
        _inventory.GetNetReservedByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
        _inventory.GetNetLegacyExitByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
    }

    [Fact]
    public async Task Confirmed_ReleasesExactlyTheNetReservation_NotTheCurrentItemQuantities()
    {
        // Items say 3 and 2, but 4 and 1 were actually reserved for the order.
        var (order, cancelled) = CancelledOrder(OrderStatus.Confirmed);
        _balanceA.Reserve(6); // 4 for this order + 2 for another order
        _balanceB.Reserve(1);
        _inventory.GetNetReservedByProductAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [_productA] = 4, [_productB] = 1 });

        await Handler().HandleAsync(cancelled);

        Assert.Equal(2, _balanceA.ReservedQuantity);
        Assert.Equal(0, _balanceB.ReservedQuantity);
        Assert.Equal(10, _balanceA.QuantityOnHand);
        Assert.All(_movements, movement => Assert.Equal(StockMovementType.OrderReservationRelease, movement.MovementType));
        Assert.Equal(4, _movements.Single(movement => movement.ProductId == _productA).Quantity);
        Assert.Equal(1, _movements.Single(movement => movement.ProductId == _productB).Quantity);
        Assert.All(_movements, movement => Assert.Equal(order.Id, movement.OrderId));
    }

    [Fact]
    public async Task Draft_DoesNotTouchStockOrShipments()
    {
        var (_, cancelled) = CancelledOrder(OrderStatus.Draft);

        await Handler().HandleAsync(cancelled);

        Assert.Empty(_movements);
        await _inventory.DidNotReceive().GetNetReservedByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _shipments.DidNotReceive().GetTrackedByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LegacyOrder_WithoutReservation_ReturnsTheStockIssuedAtConfirmation()
    {
        var (order, cancelled) = CancelledOrder(OrderStatus.Confirmed);
        _balanceA.Issue(3); // issued at confirmation by the pre-reservation flow
        _inventory.GetNetLegacyExitByProductAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [_productA] = 3 });

        await Handler().HandleAsync(cancelled);

        Assert.Equal(10, _balanceA.QuantityOnHand);
        Assert.Equal(0, _balanceA.ReservedQuantity);
        var returned = Assert.Single(_movements);
        Assert.Equal(StockMovementType.OrderCancellationReturn, returned.MovementType);
        Assert.Equal(3, returned.Quantity);
    }

    [Fact]
    public async Task LegacyOrder_WithNothingRecorded_OnlyLogs()
    {
        var (_, cancelled) = CancelledOrder(OrderStatus.Confirmed);

        var exception = await Record.ExceptionAsync(() => Handler().HandleAsync(cancelled));

        Assert.Null(exception);
        Assert.Empty(_movements);
        Assert.Equal(0, _balanceA.ReservedQuantity);
        Assert.Contains(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
    }

    [Fact]
    public async Task ReprocessingTheEvent_DoesNotReleaseTwice()
    {
        var (order, cancelled) = CancelledOrder(OrderStatus.Confirmed);
        _balanceA.Reserve(3);
        // The second read reflects the release movement written by the first run.
        _inventory.GetNetReservedByProductAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(
                new Dictionary<Guid, int> { [_productA] = 3 },
                new Dictionary<Guid, int> { [_productA] = 0 });
        var handler = Handler();

        await handler.HandleAsync(cancelled);
        await handler.HandleAsync(cancelled);

        Assert.Single(_movements);
        Assert.Equal(0, _balanceA.ReservedQuantity);
        Assert.Equal(10, _balanceA.QuantityOnHand);
    }

    [Fact]
    public async Task PendingShipment_IsCancelledWithTheOrder()
    {
        var (order, cancelled) = CancelledOrder(OrderStatus.Confirmed);
        var shipment = Shipment.Create(order.Id, order.OrderNumber, order.CustomerId, order.CustomerName, "Acme", null, null, null);
        _shipments.GetTrackedByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        await Handler().HandleAsync(cancelled);

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.Contains(shipment.History, entry => entry.NewStatus == ShipmentStatus.Cancelled);
    }

    [Fact]
    public async Task WithoutShipment_DoesNotFail()
    {
        var (_, cancelled) = CancelledOrder(OrderStatus.Confirmed);
        _shipments.GetTrackedByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var exception = await Record.ExceptionAsync(() => Handler().HandleAsync(cancelled));

        Assert.Null(exception);
    }

    private OrderCancelledHandler Handler() => new(_orders, _inventory, _shipments, _currentUser, _logger);

    private (Order Order, OrderCancelledDomainEvent Event) CancelledOrder(OrderStatus previousStatus)
    {
        var order = Order.Create(
            "ORD-CANCEL-1",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [
                OrderItem.Create(_productA, "SKU-A", "Product A", 3, 10m),
                OrderItem.Create(_productB, "SKU-B", "Product B", 2, 5m)
            ]);
        if (previousStatus == OrderStatus.Confirmed)
        {
            order.Confirm();
        }

        order.ClearDomainEvents();
        order.Cancel("Customer changed plans");
        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return (order, Assert.IsType<OrderCancelledDomainEvent>(Assert.Single(order.DomainEvents)));
    }
}
