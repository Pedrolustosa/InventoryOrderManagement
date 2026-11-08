using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Shipments;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Shipments;

public class ShipmentDispatchedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();
    private readonly IFinancialTitleRepository _titles = Substitute.For<IFinancialTitleRepository>();
    private readonly Guid _productA = Guid.NewGuid();
    private readonly Guid _productB = Guid.NewGuid();
    private readonly Guid _shipmentId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_WithReservation_ConsumesAllItems_AndCreatesOneReceivable()
    {
        var order = CreateConfirmedOrder();
        var (balanceA, balanceB) = ArrangeBalances(onHand: 10, reservedA: 2, reservedB: 3);
        ArrangeOrder(order, new Dictionary<Guid, int> { [_productA] = 2, [_productB] = 3 });

        await CreateHandler(defaultDays: 45).HandleAsync(DispatchedEvent(order));

        Assert.Equal(8, balanceA.QuantityOnHand);
        Assert.Equal(0, balanceA.ReservedQuantity);
        Assert.Equal(7, balanceB.QuantityOnHand);
        Assert.Equal(0, balanceB.ReservedQuantity);
        await _inventory.Received(2).AddMovementAsync(
            Arg.Is<StockMovement>(movement =>
                movement.MovementType == StockMovementType.OrderExit
                && movement.ShipmentId == _shipmentId
                && movement.OrderId == order.Id),
            Arg.Any<CancellationToken>());

        await _titles.Received(1).AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
        var title = (FinancialTitle)_titles.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IFinancialTitleRepository.AddAsync))
            .GetArguments()[0]!;
        Assert.Equal(FinancialTitleType.Receivable, title.Type);
        Assert.Equal(FinancialSourceType.SalesOrder, title.SourceType);
        Assert.Equal(order.Id, title.SourceId);
        Assert.Equal(order.CustomerId, title.CounterpartyId);
        Assert.Equal(order.Subtotal, title.TotalAmount);
        var installment = Assert.Single(title.Installments);
        Assert.Equal(new DateOnly(2026, 12, 23), installment.DueDate);
        Assert.Equal(order.Subtotal, installment.Amount);
    }

    [Fact]
    public async Task HandleAsync_LegacyOrderWithoutReservation_IssuesOnHandDirectly()
    {
        var order = CreateConfirmedOrder();
        // Another order reserved 4 units of product A: the legacy order must not consume that reservation.
        var (balanceA, balanceB) = ArrangeBalances(onHand: 10, reservedA: 4, reservedB: 0);
        ArrangeOrder(order, new Dictionary<Guid, int>());

        await CreateHandler().HandleAsync(DispatchedEvent(order));

        Assert.Equal(8, balanceA.QuantityOnHand);
        Assert.Equal(4, balanceA.ReservedQuantity);
        Assert.Equal(7, balanceB.QuantityOnHand);
        await _inventory.Received(2).AddMovementAsync(
            Arg.Is<StockMovement>(movement => movement.MovementType == StockMovementType.OrderExit),
            Arg.Any<CancellationToken>());
        await _titles.Received(1).AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LegacyOrderWithoutReservation_WhenAvailableIsShort_Throws()
    {
        var order = CreateConfirmedOrder();
        var (balanceA, _) = ArrangeBalances(onHand: 10, reservedA: 9, reservedB: 0);
        ArrangeOrder(order, new Dictionary<Guid, int>());

        var exception = await Assert.ThrowsAsync<OrderInsufficientStockException>(
            () => CreateHandler().HandleAsync(DispatchedEvent(order)));

        Assert.Equal("Product A", exception.ProductName);
        Assert.Equal(1, exception.AvailableQuantity);
        Assert.Equal(10, balanceA.QuantityOnHand);
    }

    [Fact]
    public async Task HandleAsync_LegacyOrderIssuedAtConfirmation_DoesNotIssueAgain_ButCreatesReceivable()
    {
        var order = CreateConfirmedOrder();
        var (balanceA, balanceB) = ArrangeBalances(onHand: 10, reservedA: 0, reservedB: 0);
        ArrangeOrder(order, new Dictionary<Guid, int>());
        _inventory.ExistsLegacyOrderExitAsync(order.Id, Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().HandleAsync(DispatchedEvent(order));

        Assert.Equal(10, balanceA.QuantityOnHand);
        Assert.Equal(10, balanceB.QuantityOnHand);
        await _inventory.DidNotReceive().AddMovementAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _titles.Received(1).AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepeatedDispatch_DoesNotDuplicateStockOrReceivable()
    {
        var order = CreateConfirmedOrder();
        var (balanceA, balanceB) = ArrangeBalances(onHand: 8, reservedA: 0, reservedB: 0);
        ArrangeOrder(order, new Dictionary<Guid, int>());
        _inventory.ExistsShipmentExitAsync(_shipmentId, Arg.Any<CancellationToken>()).Returns(true);
        _titles.ExistsReceivableForSalesOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().HandleAsync(DispatchedEvent(order));

        Assert.Equal(8, balanceA.QuantityOnHand);
        Assert.Equal(8, balanceB.QuantityOnHand);
        await _inventory.DidNotReceive().AddMovementAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _titles.DidNotReceive().AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithPartialReservation_ThrowsConflict()
    {
        var order = CreateConfirmedOrder();
        ArrangeBalances(onHand: 10, reservedA: 1, reservedB: 3);
        ArrangeOrder(order, new Dictionary<Guid, int> { [_productA] = 1, [_productB] = 3 });

        await Assert.ThrowsAsync<ShipmentConflictException>(
            () => CreateHandler().HandleAsync(DispatchedEvent(order)));

        await _titles.DidNotReceive().AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentAlreadyUsed_ThrowsDuplicateDocument()
    {
        var order = CreateConfirmedOrder();
        ArrangeBalances(onHand: 10, reservedA: 2, reservedB: 3);
        ArrangeOrder(order, new Dictionary<Guid, int> { [_productA] = 2, [_productB] = 3 });
        _titles.DocumentExistsAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DuplicateFinancialDocumentException>(
            () => CreateHandler().HandleAsync(DispatchedEvent(order)));

        await _titles.DidNotReceive().AddAsync(Arg.Any<FinancialTitle>(), Arg.Any<CancellationToken>());
    }

    private ShipmentDispatchedHandler CreateHandler(int defaultDays = 30) =>
        new(
            _orders,
            _inventory,
            _titles,
            new FixedTimeProvider(Now),
            Options.Create(new FinanceOptions { DefaultReceivableDays = defaultDays }));

    private ShipmentDispatchedDomainEvent DispatchedEvent(Order order) =>
        new(
            _shipmentId,
            order.Id,
            order.Items.Select(item => new ShipmentDispatchedItem(item.ProductId, item.Quantity)).ToList(),
            Now.UtcDateTime);

    private Order CreateConfirmedOrder()
    {
        var order = Order.Create(
            "ORD-DISPATCH-1",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [
                OrderItem.Create(_productA, "SKU-A", "Product A", 2, 10m),
                OrderItem.Create(_productB, "SKU-B", "Product B", 3, 5m)
            ]);
        order.Confirm();
        order.ClearDomainEvents();
        return order;
    }

    private (InventoryBalance A, InventoryBalance B) ArrangeBalances(int onHand, int reservedA, int reservedB)
    {
        var balanceA = InventoryBalance.Create(_productA);
        balanceA.Receive(onHand);
        if (reservedA > 0)
        {
            balanceA.Reserve(reservedA);
        }

        var balanceB = InventoryBalance.Create(_productB);
        balanceB.Receive(onHand);
        if (reservedB > 0)
        {
            balanceB.Reserve(reservedB);
        }

        _inventory.GetTrackedByProductIdAsync(_productA, Arg.Any<CancellationToken>()).Returns(balanceA);
        _inventory.GetTrackedByProductIdAsync(_productB, Arg.Any<CancellationToken>()).Returns(balanceB);
        return (balanceA, balanceB);
    }

    private void ArrangeOrder(Order order, IReadOnlyDictionary<Guid, int> netReserved)
    {
        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetNetReservedByProductAsync(order.Id, Arg.Any<CancellationToken>()).Returns(netReserved);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
