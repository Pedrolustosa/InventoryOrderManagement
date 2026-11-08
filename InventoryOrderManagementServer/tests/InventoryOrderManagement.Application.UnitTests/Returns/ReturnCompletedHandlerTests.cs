using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Returns;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Returns;

public class ReturnCompletedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly IReturnRequestRepository _returns = Substitute.For<IReturnRequestRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IPurchaseOrderRepository _purchaseOrders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();
    private readonly IFinancialTitleRepository _titles = Substitute.For<IFinancialTitleRepository>();
    private readonly List<StockMovement> _movements = [];
    private readonly List<FinancialTitle> _createdTitles = [];
    private readonly List<FinancialSettlement> _stagedSettlements = [];
    private readonly Guid _productA = Guid.NewGuid();
    private readonly Guid _productB = Guid.NewGuid();
    private readonly InventoryBalance _balanceA;
    private readonly InventoryBalance _balanceB;

    public ReturnCompletedHandlerTests()
    {
        _balanceA = InventoryBalance.Create(_productA);
        _balanceA.Receive(10);
        _balanceB = InventoryBalance.Create(_productB);
        _balanceB.Receive(10);

        _inventory.GetTrackedByProductIdAsync(_productA, Arg.Any<CancellationToken>()).Returns(_balanceA);
        _inventory.GetTrackedByProductIdAsync(_productB, Arg.Any<CancellationToken>()).Returns(_balanceB);
        _inventory.ExistsReturnMovementAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => _movements.Count > 0);
        _inventory.AddMovementAsync(Arg.Do<StockMovement>(_movements.Add), Arg.Any<CancellationToken>());
        _titles.AddAsync(Arg.Do<FinancialTitle>(_createdTitles.Add), Arg.Any<CancellationToken>());
        _titles.StageSettlement(Arg.Do<FinancialSettlement>(_stagedSettlements.Add));
        _titles.ExistsForSourceAsync(FinancialSourceType.Return, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => _createdTitles.Count > 0);
    }

    [Fact]
    public async Task CustomerReturn_WithOpenReceivable_AbatesTheReturnedValue()
    {
        var order = DeliveredOrder(quantity: 4);
        var receivable = Receivable(order, 200m);
        var (request, domainEvent) = CompletedCustomerReturn(order, requested: 3, restocked: 3);

        await Handler().HandleAsync(domainEvent);

        Assert.Equal(13, _balanceA.QuantityOnHand);
        var entry = Assert.Single(_movements);
        Assert.Equal(StockMovementType.CustomerReturnEntry, entry.MovementType);
        Assert.Equal(3, order.Items.Single().ReturnedQuantity);

        var credit = Assert.Single(_stagedSettlements);
        Assert.Equal(FinancialPaymentMethod.ReturnCredit, credit.PaymentMethod);
        Assert.Equal(150m, credit.SettledPrincipalAmount);
        Assert.Equal(0m, credit.CashAmount);
        Assert.Equal(50m, receivable.OutstandingBalance);
        Assert.Null(receivable.CancelledAtUtc);
        Assert.Empty(_createdTitles);

        Assert.Equal(ReturnFinancialStatus.Processed, request.FinancialStatus);
        Assert.Equal(receivable.Id, request.AdjustedFinancialTitleId);
        Assert.Equal(150m, request.FinancialAdjustedAmount);
        Assert.Null(request.FinancialTitleId);
    }

    [Fact]
    public async Task CustomerReturn_OfTheWholeUnpaidReceivable_CancelsIt()
    {
        var order = DeliveredOrder(quantity: 4);
        var receivable = Receivable(order, 200m);
        var (request, domainEvent) = CompletedCustomerReturn(order, requested: 4, restocked: 4);

        await Handler().HandleAsync(domainEvent);

        Assert.NotNull(receivable.CancelledAtUtc);
        Assert.Empty(_stagedSettlements);
        Assert.Empty(_createdTitles);
        Assert.Equal(200m, request.FinancialAdjustedAmount);
    }

    [Fact]
    public async Task CustomerReturn_WithPaidReceivable_CreatesRefundPayable()
    {
        var order = DeliveredOrder(quantity: 4);
        var receivable = Receivable(order, 200m);
        receivable.SettleInstallment(
            receivable.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Pix,
            200m,
            0m,
            0m,
            0m,
            Now.UtcDateTime,
            null,
            null,
            null,
            Now.UtcDateTime,
            null);
        var (request, domainEvent) = CompletedCustomerReturn(order, requested: 3, restocked: 3);

        await Handler(returnTitleDays: 10).HandleAsync(domainEvent);

        Assert.Empty(_stagedSettlements);
        var refund = Assert.Single(_createdTitles);
        Assert.Equal(FinancialTitleType.Payable, refund.Type);
        Assert.Equal(FinancialCounterpartyType.Customer, refund.CounterpartyType);
        Assert.Equal(order.CustomerId, refund.CounterpartyId);
        Assert.Equal(FinancialSourceType.Return, refund.SourceType);
        Assert.Equal(request.Id, refund.SourceId);
        Assert.Equal(150m, refund.TotalAmount);
        Assert.Equal(new DateOnly(2026, 11, 18), refund.Installments.Single().DueDate);
        Assert.Null(request.AdjustedFinancialTitleId);
        Assert.Equal(refund.Id, request.FinancialTitleId);
    }

    [Fact]
    public async Task SupplierReturn_WithOpenPayable_AbatesItAndIssuesStock()
    {
        var purchaseOrder = ReceivedPurchaseOrder(quantity: 10, unitCost: 10m);
        var payable = Payable(purchaseOrder, 100m);
        var (request, domainEvent) = DispatchedSupplierReturn(purchaseOrder, quantity: 4);

        await Handler().HandleAsync(domainEvent);

        Assert.Equal(6, _balanceA.QuantityOnHand);
        Assert.Equal(StockMovementType.SupplierReturnExit, Assert.Single(_movements).MovementType);
        Assert.Equal(4, purchaseOrder.Items.Single().SupplierReturnedQuantity);
        Assert.Equal(40m, Assert.Single(_stagedSettlements).SettledPrincipalAmount);
        Assert.Equal(60m, payable.OutstandingBalance);
        Assert.Empty(_createdTitles);
        Assert.Equal(payable.Id, request.AdjustedFinancialTitleId);
    }

    [Fact]
    public async Task SupplierReturn_WithoutOpenPayable_CreatesReceivableFromSupplier()
    {
        var purchaseOrder = ReceivedPurchaseOrder(quantity: 10, unitCost: 10m);
        var (request, domainEvent) = DispatchedSupplierReturn(purchaseOrder, quantity: 4);

        await Handler().HandleAsync(domainEvent);

        var receivable = Assert.Single(_createdTitles);
        Assert.Equal(FinancialTitleType.Receivable, receivable.Type);
        Assert.Equal(FinancialCounterpartyType.Supplier, receivable.CounterpartyType);
        Assert.Equal(purchaseOrder.SupplierId, receivable.CounterpartyId);
        Assert.Equal(40m, receivable.TotalAmount);
        Assert.Equal(receivable.Id, request.FinancialTitleId);
    }

    [Fact]
    public async Task SupplierReturn_AboveAvailableStock_Throws()
    {
        var purchaseOrder = ReceivedPurchaseOrder(quantity: 10, unitCost: 10m);
        _balanceA.Reserve(8);
        var (_, domainEvent) = DispatchedSupplierReturn(purchaseOrder, quantity: 4);

        await Assert.ThrowsAsync<OrderInsufficientStockException>(() => Handler().HandleAsync(domainEvent));
        Assert.Empty(_movements);
    }

    [Fact]
    public async Task Exchange_WithoutPriceDifference_MovesStockWithoutFinance()
    {
        var order = DeliveredOrder(quantity: 4);
        Receivable(order, 200m);
        // Returns 2 × 50.00 and receives 1 × 100.00.
        var (request, domainEvent) = CompletedExchange(order, returned: 2, replacementPrice: 100m);

        await Handler().HandleAsync(domainEvent);

        Assert.Equal(12, _balanceA.QuantityOnHand);
        Assert.Equal(9, _balanceB.QuantityOnHand);
        Assert.Contains(_movements, movement => movement.MovementType == StockMovementType.CustomerReturnEntry);
        var exit = Assert.Single(_movements, movement => movement.MovementType == StockMovementType.ExchangeReplacementExit);
        Assert.Equal(_productB, exit.ProductId);
        Assert.Empty(_stagedSettlements);
        Assert.Empty(_createdTitles);
        Assert.Equal(ReturnFinancialStatus.Processed, request.FinancialStatus);
    }

    [Fact]
    public async Task Exchange_WithHigherReplacementPrice_CreatesReceivableForTheDifference()
    {
        var order = DeliveredOrder(quantity: 4);
        var (request, domainEvent) = CompletedExchange(order, returned: 2, replacementPrice: 130m);

        await Handler().HandleAsync(domainEvent);

        var difference = Assert.Single(_createdTitles);
        Assert.Equal(FinancialTitleType.Receivable, difference.Type);
        Assert.Equal(FinancialCounterpartyType.Customer, difference.CounterpartyType);
        Assert.Equal(30m, difference.TotalAmount);
        Assert.Equal(difference.Id, request.FinancialTitleId);
    }

    [Fact]
    public async Task CustomerReturn_DamagedItems_AreRecordedWithoutReachingAvailableStock()
    {
        var order = DeliveredOrder(quantity: 4);
        var (request, domainEvent) = CompletedCustomerReturn(order, requested: 3, restocked: 0, quarantined: 1, discarded: 2);

        await Handler().HandleAsync(domainEvent);

        Assert.Equal(10, _balanceA.QuantityOnHand);
        Assert.Equal(10, _balanceA.AvailableQuantity);
        var quarantine = Assert.Single(_movements, movement => movement.MovementType == StockMovementType.CustomerReturnQuarantine);
        var loss = Assert.Single(_movements, movement => movement.MovementType == StockMovementType.CustomerReturnLoss);
        Assert.Equal(1, quarantine.Quantity);
        Assert.Equal(2, loss.Quantity);
        Assert.Equal(quarantine.PreviousQuantity, quarantine.NewQuantity);
        Assert.DoesNotContain(_movements, movement => movement.MovementType == StockMovementType.CustomerReturnEntry);
        // Accepted damaged units are still refunded (no receivable → refund payable of 3 × 50.00).
        Assert.Equal(150m, Assert.Single(_createdTitles).TotalAmount);
        Assert.Equal(3, order.Items.Single().ReturnedQuantity);
    }

    [Fact]
    public async Task RepeatedCompletion_DoesNotDuplicateStockOrFinance()
    {
        var order = DeliveredOrder(quantity: 4);
        var receivable = Receivable(order, 200m);
        var (_, domainEvent) = CompletedCustomerReturn(order, requested: 3, restocked: 3);
        var handler = Handler();

        await handler.HandleAsync(domainEvent);
        await handler.HandleAsync(domainEvent);

        Assert.Single(_movements);
        Assert.Equal(13, _balanceA.QuantityOnHand);
        Assert.Equal(3, order.Items.Single().ReturnedQuantity);
        Assert.Single(_stagedSettlements);
        Assert.Equal(50m, receivable.OutstandingBalance);
        Assert.Empty(_createdTitles);
    }

    [Fact]
    public async Task CustomerReturn_WithTreatmentNone_MovesStockOnly()
    {
        var order = DeliveredOrder(quantity: 4);
        Receivable(order, 200m);
        var (request, domainEvent) = CompletedCustomerReturn(
            order,
            requested: 3,
            restocked: 3,
            treatment: ReturnFinancialTreatment.None);

        await Handler().HandleAsync(domainEvent);

        Assert.Single(_movements);
        Assert.Empty(_stagedSettlements);
        Assert.Empty(_createdTitles);
        Assert.Equal(ReturnFinancialStatus.None, request.FinancialStatus);
    }

    private ReturnCompletedHandler Handler(int returnTitleDays = 30) =>
        new(
            _returns,
            _orders,
            _purchaseOrders,
            _inventory,
            _titles,
            new FixedTimeProvider(Now),
            Options.Create(new FinanceOptions { DefaultReturnTitleDays = returnTitleDays }));

    private Order DeliveredOrder(int quantity)
    {
        var order = Order.Create(
            "ORD-RET-1",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(_productA, "SKU-A", "Product A", quantity, 50m)]);
        order.Confirm();
        order.Ship();
        order.Deliver();
        order.ClearDomainEvents();
        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    private PurchaseOrder ReceivedPurchaseOrder(int quantity, decimal unitCost)
    {
        var purchaseOrder = PurchaseOrder.Create(
            "PO-RET-1",
            Guid.NewGuid(),
            "Acme Supply",
            null,
            [PurchaseOrderItem.Create(_productA, "SKU-A", "Product A", quantity, unitCost)]);
        purchaseOrder.Send();
        purchaseOrder.Receive([(purchaseOrder.Items.Single().Id, quantity)]);
        _purchaseOrders.GetTrackedByIdAsync(purchaseOrder.Id, Arg.Any<CancellationToken>()).Returns(purchaseOrder);
        return purchaseOrder;
    }

    private FinancialTitle Receivable(Order order, decimal amount)
    {
        var title = FinancialTitle.Create(
            "FT-RECV-1",
            FinancialTitleType.Receivable,
            "Receivable",
            FinancialCounterpartyType.Customer,
            order.CustomerId,
            order.CustomerName,
            FinancialSourceType.SalesOrder,
            order.Id,
            order.OrderNumber,
            null,
            new DateOnly(2026, 11, 1),
            amount,
            null,
            null,
            [(1, new DateOnly(2026, 12, 1), amount)],
            Now.UtcDateTime);
        _titles.FindActiveSourceTitleAsync(
                FinancialTitleType.Receivable,
                FinancialSourceType.SalesOrder,
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns(title);
        return title;
    }

    private FinancialTitle Payable(PurchaseOrder purchaseOrder, decimal amount)
    {
        var title = FinancialTitle.Create(
            "FT-PAY-1",
            FinancialTitleType.Payable,
            "Payable",
            FinancialCounterpartyType.Supplier,
            purchaseOrder.SupplierId,
            purchaseOrder.SupplierName,
            FinancialSourceType.PurchaseOrder,
            purchaseOrder.Id,
            purchaseOrder.PurchaseOrderNumber,
            null,
            new DateOnly(2026, 11, 1),
            amount,
            null,
            null,
            [(1, new DateOnly(2026, 12, 1), amount)],
            Now.UtcDateTime);
        _titles.FindActiveSourceTitleAsync(
                FinancialTitleType.Payable,
                FinancialSourceType.PurchaseOrder,
                purchaseOrder.Id,
                Arg.Any<CancellationToken>())
            .Returns(title);
        return title;
    }

    private (ReturnRequest Request, ReturnCompletedDomainEvent Event) CompletedCustomerReturn(
        Order order,
        int requested,
        int restocked,
        int quarantined = 0,
        int discarded = 0,
        ReturnFinancialTreatment treatment = ReturnFinancialTreatment.CustomerRefund)
    {
        var request = ReturnRequest.CreateCustomer(
            "RET-1",
            order.CustomerId,
            order.CustomerName,
            order.Id,
            order.OrderNumber,
            ReturnReason.Defective,
            null,
            null,
            treatment,
            null,
            null,
            [CustomerItem(order, requested)]);
        return Complete(request, restocked, quarantined, discarded);
    }

    private (ReturnRequest Request, ReturnCompletedDomainEvent Event) CompletedExchange(
        Order order,
        int returned,
        decimal replacementPrice)
    {
        var request = ReturnRequest.CreateExchange(
            "RET-EX-1",
            order.CustomerId,
            order.CustomerName,
            order.Id,
            order.OrderNumber,
            ReturnReason.IncorrectProduct,
            null,
            null,
            ReturnFinancialTreatment.CustomerRefund,
            null,
            null,
            [CustomerItem(order, returned)],
            [ReturnReplacementItem.Create(_productB, "SKU-B", "Product B", 1, replacementPrice)]);
        return Complete(request, returned, 0, 0);
    }

    private (ReturnRequest Request, ReturnCompletedDomainEvent Event) DispatchedSupplierReturn(
        PurchaseOrder purchaseOrder,
        int quantity)
    {
        var line = purchaseOrder.Items.Single();
        var request = ReturnRequest.CreateSupplier(
            "RET-SUP-1",
            purchaseOrder.SupplierId,
            purchaseOrder.SupplierName,
            purchaseOrder.Id,
            purchaseOrder.PurchaseOrderNumber,
            ReturnReason.Damaged,
            null,
            null,
            ReturnFinancialTreatment.SupplierCredit,
            null,
            null,
            [ReturnItem.CreateForSupplier(line.ProductId, line.ProductSku, line.ProductName, line.Id, line.ReceivedQuantity, 0, quantity, line.UnitCost)]);
        request.Authorize(null, null);
        request.Dispatch(null, null);
        _returns.GetTrackedByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        return (request, Assert.IsType<ReturnCompletedDomainEvent>(Assert.Single(request.DomainEvents)));
    }

    private static ReturnItem CustomerItem(Order order, int requested)
    {
        var line = order.Items.Single();
        return ReturnItem.CreateForCustomer(
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Id,
            line.Quantity,
            line.ReturnedQuantity,
            requested,
            line.UnitPrice);
    }

    private (ReturnRequest Request, ReturnCompletedDomainEvent Event) Complete(
        ReturnRequest request,
        int restocked,
        int quarantined,
        int discarded)
    {
        var item = request.Items.Single();
        request.Receive([(item.Id, item.RequestedQuantity)], null);
        request.StartInspection();
        request.CompleteInspection(
            [
                new InspectionLine(
                    item.Id,
                    quarantined + discarded > 0 ? ReturnItemCondition.Damaged : ReturnItemCondition.Good,
                    restocked,
                    quarantined,
                    discarded,
                    item.RequestedQuantity - restocked - quarantined - discarded,
                    null)
            ],
            null,
            null);
        _returns.GetTrackedByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        return (request, Assert.IsType<ReturnCompletedDomainEvent>(Assert.Single(request.DomainEvents)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
