using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Orders;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Orders;

public class GetOrderTimelineUseCaseTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly IOrderTimelineQuery _query = Substitute.For<IOrderTimelineQuery>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _shipmentId = Guid.NewGuid();
    private readonly Guid _receivableId = Guid.NewGuid();
    private readonly Guid _returnId = Guid.NewGuid();
    private readonly Guid _operatorId = Guid.NewGuid();

    [Fact]
    public async Task FullFlow_ProducesEveryEntry_InChronologicalAndLogicalOrder()
    {
        GrantPermissions(Permissions.Orders.View, Permissions.Finance.View, Permissions.Returns.View);
        _query.LoadAsync(_orderId, true, true, Arg.Any<CancellationToken>()).Returns(FullFlowSources());

        var timeline = await UseCase().ExecuteAsync(_orderId);

        Assert.Equal(_orderId, timeline.OrderId);
        Assert.Equal("ORD-1", timeline.OrderNumber);
        Assert.Equal(OrderStatus.Delivered, timeline.CurrentStatus);
        Assert.Equal(
        [
            "order.created",
            "order.confirmed",
            "stock.reserved",
            "shipment.created",
            "shipment.dispatched",
            "stock.issued",
            "finance.receivable_created",
            "order.shipped",
            "shipment.in_transit",
            "shipment.delivered",
            "order.delivered",
            "finance.settled",
            "finance.settled",
            "return.requested",
            "return.received",
            "return.completed",
            "stock.return_restocked",
            "finance.abated"
        ], timeline.Entries.Select(entry => entry.EventType));

        Assert.Equal(timeline.Entries.OrderBy(entry => entry.OccurredAtUtc), timeline.Entries, ReferenceEqualityComparer.Instance);

        var settlements = timeline.Entries.Where(entry => entry.EventType == "finance.settled").ToList();
        Assert.Equal("PartiallyPaid", settlements[0].Status);
        Assert.Equal(60m, settlements[0].Summary.Amount);
        Assert.Equal("Paid", settlements[1].Status);
        Assert.Equal(FinancialPaymentMethod.Pix, settlements[1].Summary.PaymentMethod);
        Assert.Equal(_operatorId, settlements[1].PerformedBy?.UserId);
        Assert.Equal("Olivia Operator", settlements[1].PerformedBy?.DisplayName);

        var issued = timeline.Entries.Single(entry => entry.EventType == "stock.issued");
        Assert.Equal(OrderTimelineModule.Stock, issued.Module);
        Assert.Equal("Product", issued.ReferenceType);
        Assert.Equal(_productId, issued.ReferenceId);
        Assert.Equal(2, issued.Summary.Quantity);
        Assert.Equal(_shipmentId, issued.Summary.ShipmentId);

        var receivable = timeline.Entries.Single(entry => entry.EventType == "finance.receivable_created");
        Assert.Equal("FinancialTitle", receivable.ReferenceType);
        Assert.Equal(FinancialTitleType.Receivable, receivable.Summary.TitleType);
        Assert.Equal(new DateOnly(2026, 12, 1), receivable.Summary.DueDate);
        Assert.Null(receivable.PerformedBy);

        Assert.All(
            timeline.Entries.Where(entry => entry.Module == OrderTimelineModule.Order),
            entry => Assert.Null(entry.PerformedBy));
    }

    [Fact]
    public async Task TiesAtTheSameInstant_FollowTheLogicalFlow()
    {
        GrantPermissions(Permissions.Orders.View);
        var sources = DraftSources(OrderStatus.Confirmed, confirmedAt: T0.AddMinutes(5));
        _query.LoadAsync(_orderId, false, false, Arg.Any<CancellationToken>()).Returns(new OrderTimelineSources
        {
            Order = sources.Order,
            // Recorded at exactly the confirmation instant and listed first on purpose.
            Movements = [Movement(StockMovementType.OrderReservation, T0.AddMinutes(5))]
        });

        var timeline = await UseCase().ExecuteAsync(_orderId);

        Assert.Equal(["order.created", "order.confirmed", "stock.reserved"], timeline.Entries.Select(entry => entry.EventType));
    }

    [Fact]
    public async Task DraftOrder_OnlyHasTheCreatedEntry()
    {
        GrantPermissions(Permissions.Orders.View, Permissions.Finance.View, Permissions.Returns.View);
        _query.LoadAsync(_orderId, true, true, Arg.Any<CancellationToken>()).Returns(DraftSources(OrderStatus.Draft));

        var timeline = await UseCase().ExecuteAsync(_orderId);

        var entry = Assert.Single(timeline.Entries);
        Assert.Equal("order.created", entry.EventType);
        Assert.Equal("Draft", entry.Status);
        Assert.Equal(OrderTimelineModule.Order, entry.Module);
        Assert.Equal("Order", entry.ReferenceType);
        Assert.Equal(_orderId, entry.ReferenceId);
    }

    [Fact]
    public async Task WithoutFinanceAndReturnPermissions_DoesNotLoadOrReturnThoseEntries()
    {
        GrantPermissions(Permissions.Orders.View);
        // Even if a query implementation ignored the flags, the use case must not leak those modules.
        _query.LoadAsync(_orderId, false, false, Arg.Any<CancellationToken>()).Returns(FullFlowSources());

        var timeline = await UseCase().ExecuteAsync(_orderId);

        await _query.Received(1).LoadAsync(_orderId, false, false, Arg.Any<CancellationToken>());
        Assert.DoesNotContain(timeline.Entries, entry => entry.Module is OrderTimelineModule.Finance or OrderTimelineModule.Return);
        Assert.Contains(timeline.Entries, entry => entry.Module == OrderTimelineModule.Stock);
        Assert.Contains(timeline.Entries, entry => entry.Module == OrderTimelineModule.Shipment);
    }

    [Fact]
    public async Task WithFinanceButNotReturns_KeepsFinanceAndDropsReturnEntries()
    {
        GrantPermissions(Permissions.Orders.View, Permissions.Finance.View);
        _query.LoadAsync(_orderId, true, false, Arg.Any<CancellationToken>()).Returns(FullFlowSources());

        var timeline = await UseCase().ExecuteAsync(_orderId);

        Assert.Contains(timeline.Entries, entry => entry.Module == OrderTimelineModule.Finance);
        Assert.DoesNotContain(timeline.Entries, entry => entry.Module == OrderTimelineModule.Return);
    }

    [Fact]
    public async Task UnknownOrder_ThrowsNotFound()
    {
        GrantPermissions(Permissions.Orders.View);
        _query.LoadAsync(_orderId, false, false, Arg.Any<CancellationToken>()).Returns((OrderTimelineSources?)null);

        await Assert.ThrowsAsync<OrderNotFoundException>(() => UseCase().ExecuteAsync(_orderId));
    }

    private GetOrderTimelineUseCase UseCase() => new(_query, _currentUser);

    private void GrantPermissions(params string[] permissions) =>
        _currentUser.Permissions.Returns(new HashSet<string>(permissions));

    private OrderTimelineSources DraftSources(OrderStatus status, DateTime? confirmedAt = null) =>
        new()
        {
            Order = new OrderTimelineOrderRow(_orderId, "ORD-1", status, 100m, T0, confirmedAt, null, null, null)
        };

    private OrderTimelineMovementRow Movement(
        StockMovementType type,
        DateTime at,
        Guid? shipmentId = null,
        Guid? returnId = null) =>
        new(Guid.NewGuid(), _productId, "SKU-1", "Widget", type, 2, shipmentId, returnId, at);

    private OrderTimelineSources FullFlowSources()
    {
        var at = (int minutes) => T0.AddMinutes(minutes);
        return new OrderTimelineSources
        {
            Order = new OrderTimelineOrderRow(_orderId, "ORD-1", OrderStatus.Delivered, 100m, at(0), at(10), at(32), at(60), null),
            // Deliberately unsorted: the builder must order them.
            Movements =
            [
                Movement(StockMovementType.CustomerReturnEntry, at(120), returnId: _returnId),
                Movement(StockMovementType.OrderReservation, at(10)),
                Movement(StockMovementType.OrderExit, at(31), shipmentId: _shipmentId)
            ],
            ShipmentHistory =
            [
                new(_shipmentId, Guid.NewGuid(), "Acme", "TRK-1", ShipmentStatus.Shipped, ShipmentStatus.InTransit, at(40), _operatorId),
                new(_shipmentId, Guid.NewGuid(), "Acme", "TRK-1", null, ShipmentStatus.Preparing, at(20), _operatorId),
                new(_shipmentId, Guid.NewGuid(), "Acme", "TRK-1", ShipmentStatus.Preparing, ShipmentStatus.Shipped, at(30), _operatorId),
                new(_shipmentId, Guid.NewGuid(), "Acme", "TRK-1", ShipmentStatus.InTransit, ShipmentStatus.Delivered, at(55), _operatorId)
            ],
            Titles =
            [
                new(_receivableId, "FT-1", FinancialTitleType.Receivable, FinancialSourceType.SalesOrder, _orderId, 100m,
                    new DateOnly(2026, 12, 1), at(31), null, null)
            ],
            Settlements =
            [
                new(Guid.NewGuid(), _receivableId, FinancialPaymentMethod.ReturnCredit, 0m, at(121), null),
                new(Guid.NewGuid(), _receivableId, FinancialPaymentMethod.Pix, 40m, at(80), _operatorId),
                new(Guid.NewGuid(), _receivableId, FinancialPaymentMethod.Cash, 60m, at(70), _operatorId)
            ],
            Returns =
            [
                new(_returnId, "RET-1", ReturnType.CustomerReturn, ReturnStatus.Completed, 50m, at(100), at(110), at(120), null, _operatorId)
            ],
            UserNames = new Dictionary<Guid, string> { [_operatorId] = "Olivia Operator" }
        };
    }
}
