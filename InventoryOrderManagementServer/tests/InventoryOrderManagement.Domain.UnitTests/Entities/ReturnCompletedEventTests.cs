using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class ReturnCompletedEventTests
{
    [Fact]
    public void CompleteInspection_WithAcceptedUnits_RaisesEventOnceWithDispositions()
    {
        var request = ReceivedCustomerReturn(requested: 4);
        var item = request.Items.Single();

        request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Damaged, 1, 1, 1, 1, null)], null, null);

        var raised = Assert.IsType<ReturnCompletedDomainEvent>(Assert.Single(request.DomainEvents));
        Assert.Equal(request.Id, raised.ReturnRequestId);
        Assert.Equal(ReturnType.CustomerReturn, raised.Type);
        Assert.Equal(request.OrderId, raised.OrderId);
        Assert.Null(raised.PurchaseOrderId);
        var line = Assert.Single(raised.Items);
        Assert.Equal(item.ProductId, line.ProductId);
        Assert.Equal(3, line.Quantity);
        Assert.Equal((1, 1, 1), (line.RestockedQuantity, line.QuarantinedQuantity, line.DiscardedQuantity));
        Assert.Equal(ReturnItemCondition.Damaged, line.Condition);
        Assert.Equal(50m, line.UnitValue);
        Assert.Empty(raised.Replacements);
    }

    [Fact]
    public void CompleteInspection_AllRejected_DoesNotRaiseEvent()
    {
        var request = ReceivedCustomerReturn(requested: 2);
        var item = request.Items.Single();

        request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Damaged, 0, 0, 0, 2, null)], null, null);

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Empty(request.DomainEvents);
    }

    [Fact]
    public void InvalidTransitions_DoNotRaiseEvent()
    {
        var request = ReceivedCustomerReturn(requested: 2);
        var item = request.Items.Single();
        request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Good, 2, 0, 0, 0, null)], null, null);
        request.ClearDomainEvents();

        Assert.Throws<InvalidReturnStatusException>(() =>
            request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Good, 2, 0, 0, 0, null)], null, null));
        Assert.Throws<InvalidReturnStatusException>(() => request.Cancel("Too late"));
        Assert.Empty(request.DomainEvents);

        var draft = ReturnRequest.CreateCustomer(
            "RET-2", Guid.NewGuid(), "Jane", Guid.NewGuid(), "ORD-2", ReturnReason.Defective, null, null,
            ReturnFinancialTreatment.CustomerRefund, null, null, [CustomerItem(2)]);
        draft.Cancel("Customer gave up");
        Assert.Empty(draft.DomainEvents);
    }

    [Fact]
    public void Dispatch_SupplierReturn_RaisesEventOnceWithDispatchedQuantities()
    {
        var purchaseItemId = Guid.NewGuid();
        var request = ReturnRequest.CreateSupplier(
            "RET-SUP", Guid.NewGuid(), "Acme", Guid.NewGuid(), "PO-1", ReturnReason.Damaged, null, null,
            ReturnFinancialTreatment.SupplierCredit, null, null,
            [ReturnItem.CreateForSupplier(Guid.NewGuid(), "SKU", "Widget", purchaseItemId, 10, 0, 4, 10m)]);

        Assert.Throws<InvalidReturnStatusException>(() => request.Dispatch(null, null));
        Assert.Empty(request.DomainEvents);

        request.Authorize(null, null);
        request.Dispatch(null, "TRK");

        var raised = Assert.IsType<ReturnCompletedDomainEvent>(Assert.Single(request.DomainEvents));
        Assert.Equal(ReturnType.SupplierReturn, raised.Type);
        Assert.Equal(request.PurchaseOrderId, raised.PurchaseOrderId);
        Assert.Equal(4, Assert.Single(raised.Items).Quantity);
    }

    [Fact]
    public void CreateExchange_RequiresReplacements_AndEventCarriesThem()
    {
        Assert.Throws<DomainException>(() => Exchange([]));

        var replacement = ReturnReplacementItem.Create(Guid.NewGuid(), "SKU-B", "Other", 1, 120m);
        var request = Exchange([replacement]);
        Assert.Equal(ReturnType.Exchange, request.Type);
        Assert.Equal(ReturnFinancialStatus.Pending, request.FinancialStatus);
        Assert.Equal(20m, request.EstimatedFinancialAmount);

        var item = request.Items.Single();
        request.Receive([(item.Id, 2)], null);
        request.StartInspection();
        request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Good, 2, 0, 0, 0, null)], null, null);

        var raised = Assert.IsType<ReturnCompletedDomainEvent>(Assert.Single(request.DomainEvents));
        var line = Assert.Single(raised.Replacements);
        Assert.Equal(replacement.ProductId, line.ProductId);
        Assert.Equal(120m, line.UnitPrice);
    }

    [Fact]
    public void MarkFinanciallyProcessed_AppliesOnce()
    {
        var request = ReceivedCustomerReturn(requested: 2);
        var item = request.Items.Single();
        Assert.Throws<InvalidReturnStatusException>(() => request.MarkFinanciallyProcessed(null, 0m, null));

        request.CompleteInspection([new InspectionLine(item.Id, ReturnItemCondition.Good, 2, 0, 0, 0, null)], null, null);
        var titleId = Guid.NewGuid();
        request.MarkFinanciallyProcessed(titleId, 100m, null);

        Assert.Equal(ReturnFinancialStatus.Processed, request.FinancialStatus);
        Assert.Equal(titleId, request.AdjustedFinancialTitleId);
        Assert.Throws<ReturnConflictException>(() => request.MarkFinanciallyProcessed(null, 0m, null));
    }

    [Fact]
    public void ReturnCreditMovements_AreRecordOnly_AndReplacementExitReducesStock()
    {
        var quarantine = StockMovement.CreateForCustomerReturn(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 5, 5, "Quarantine",
            StockMovementType.CustomerReturnQuarantine);
        Assert.Equal(StockMovementType.CustomerReturnQuarantine, quarantine.MovementType);

        Assert.Throws<DomainException>(() => StockMovement.CreateForCustomerReturn(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 5, 7, "Loss",
            StockMovementType.CustomerReturnLoss));
        Assert.Throws<DomainException>(() => StockMovement.CreateForCustomerReturn(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 5, 3, "Wrong",
            StockMovementType.SupplierReturnExit));

        var exit = StockMovement.CreateForExchangeReplacement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 5, 3, null);
        Assert.Equal(StockMovementType.ExchangeReplacementExit, exit.MovementType);
        Assert.Null(exit.ReturnItemId);
    }

    private static ReturnRequest ReceivedCustomerReturn(int requested)
    {
        var request = ReturnRequest.CreateCustomer(
            "RET-1", Guid.NewGuid(), "Jane", Guid.NewGuid(), "ORD-1", ReturnReason.Defective, null, null,
            ReturnFinancialTreatment.CustomerRefund, null, null, [CustomerItem(requested)]);
        request.Receive([(request.Items.Single().Id, requested)], null);
        request.StartInspection();
        return request;
    }

    private static ReturnRequest Exchange(IReadOnlyList<ReturnReplacementItem> replacements) =>
        ReturnRequest.CreateExchange(
            "RET-EX", Guid.NewGuid(), "Jane", Guid.NewGuid(), "ORD-1", ReturnReason.IncorrectProduct, null, null,
            ReturnFinancialTreatment.None, null, null, [CustomerItem(2)], replacements);

    private static ReturnItem CustomerItem(int requested) =>
        ReturnItem.CreateForCustomer(Guid.NewGuid(), "SKU-A", "Widget", Guid.NewGuid(), 10, 0, requested, 50m);
}
