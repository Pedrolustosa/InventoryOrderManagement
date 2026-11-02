using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class ReturnRequestTests
{
    [Fact]
    public void CustomerFlow_ReceiveInspectRestock_Completes()
    {
        var orderItemId = Guid.NewGuid();
        var item = CustomerItem(orderItemId, sourceQuantity: 10, previouslyReturned: 0, requested: 3);
        var request = CustomerReturn([item]);

        request.Receive([(item.Id, 3)], "Received");
        Assert.Equal(ReturnStatus.Received, request.Status);

        request.StartInspection();
        request.CompleteInspection(
            [new InspectionLine(item.Id, ReturnItemCondition.Good, 3, 0, 0, 0, null)],
            null,
            Guid.NewGuid());

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Equal(3, item.RestockedQuantity);
        Assert.NotNull(request.CompletedAtUtc);
    }

    [Fact]
    public void CustomerFlow_DamagedUnits_OnlyRestockedCountAsAcceptedForStock()
    {
        var item = CustomerItem(Guid.NewGuid(), 10, 0, 5);
        var request = CustomerReturn([item]);
        request.Receive([(item.Id, 5)], null);
        request.StartInspection();

        request.CompleteInspection(
            [new InspectionLine(item.Id, ReturnItemCondition.Damaged, 2, 2, 1, 0, null)],
            null,
            null);

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Equal(2, item.RestockedQuantity);
        Assert.Equal(2, item.QuarantinedQuantity);
        Assert.Equal(1, item.DiscardedQuantity);
        Assert.Equal(5, item.AcceptedQuantity);
    }

    [Fact]
    public void CustomerFlow_AllRejected_BecomesRejected()
    {
        var item = CustomerItem(Guid.NewGuid(), 10, 0, 2);
        var request = CustomerReturn([item]);
        request.Receive([(item.Id, 2)], null);
        request.StartInspection();

        request.CompleteInspection(
            [new InspectionLine(item.Id, ReturnItemCondition.Defective, 0, 0, 0, 2, null)],
            null,
            null);

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal(0, item.AcceptedQuantity);
    }

    [Fact]
    public void CustomerFlow_InspectionSumMismatch_ThrowsConflict()
    {
        var item = CustomerItem(Guid.NewGuid(), 10, 0, 2);
        var request = CustomerReturn([item]);
        request.Receive([(item.Id, 2)], null);
        request.StartInspection();

        Assert.Throws<ReturnConflictException>(() =>
            request.CompleteInspection(
                [new InspectionLine(item.Id, ReturnItemCondition.Good, 1, 0, 0, 0, null)],
                null,
                null));
    }

    [Fact]
    public void CustomerFlow_CancelAfterInspectionStart_Throws()
    {
        var item = CustomerItem(Guid.NewGuid(), 10, 0, 1);
        var request = CustomerReturn([item]);
        request.Receive([(item.Id, 1)], null);
        request.StartInspection();

        Assert.Throws<InvalidReturnStatusException>(() => request.Cancel("Too late"));
    }

    [Fact]
    public void CustomerFlow_CancelDraft_KeepsHistoryFields()
    {
        var request = CustomerReturn([CustomerItem(Guid.NewGuid(), 5, 0, 1)]);

        request.Cancel("Changed mind");

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.Equal("Changed mind", request.CancellationReason);
        Assert.NotNull(request.CancelledAtUtc);
    }

    [Fact]
    public void Create_RequestedAboveAvailable_ThrowsConflict()
    {
        Assert.Throws<ReturnConflictException>(() =>
            CustomerItem(Guid.NewGuid(), sourceQuantity: 10, previouslyReturned: 7, requested: 4));
    }

    [Fact]
    public void SupplierFlow_AuthorizeThenDispatch_SetsQuantities()
    {
        var item = SupplierItem(Guid.NewGuid(), received: 15, previouslyReturned: 3, requested: 5);
        var request = SupplierReturn([item]);

        request.Authorize("OK", "AUTH-1");
        Assert.Equal(ReturnStatus.Authorized, request.Status);

        request.Dispatch("Shipped", "NF-1");
        Assert.Equal(ReturnStatus.Dispatched, request.Status);
        Assert.Equal(5, item.DispatchedQuantity);
        Assert.NotNull(request.DispatchedAtUtc);
    }

    [Fact]
    public void SupplierFlow_CancelAfterDispatch_Throws()
    {
        var item = SupplierItem(Guid.NewGuid(), 10, 0, 2);
        var request = SupplierReturn([item]);
        request.Authorize(null, null);
        request.Dispatch(null, null);

        Assert.Throws<InvalidReturnStatusException>(() => request.Cancel("No"));
    }

    [Fact]
    public void OrderItem_RegisterCustomerReturn_RespectsAvailable()
    {
        var order = DeliveredOrder(10);
        var item = order.Items.Single();

        order.RegisterCustomerReturn([(item.Id, 7)]);
        Assert.Equal(7, item.ReturnedQuantity);
        Assert.Equal(3, item.AvailableToReturn);

        Assert.Throws<ReturnConflictException>(() => order.RegisterCustomerReturn([(item.Id, 4)]));
    }

    [Fact]
    public void PurchaseOrderItem_RegisterSupplierReturn_RespectsReceived()
    {
        var order = ReceivedPurchase(20, received: 15);
        var item = order.Items.Single();

        order.RegisterSupplierReturn([(item.Id, 5)]);
        Assert.Equal(5, item.SupplierReturnedQuantity);
        Assert.Equal(10, item.AvailableToReturnToSupplier);

        Assert.Throws<ReturnConflictException>(() => order.RegisterSupplierReturn([(item.Id, 11)]));
    }

    [Fact]
    public void ReasonOther_WithoutDescription_Throws()
    {
        Assert.Throws<DomainException>(() =>
            ReturnRequest.CreateCustomer(
                "RET-1",
                Guid.NewGuid(),
                "Customer",
                Guid.NewGuid(),
                "ORD-1",
                ReturnReason.Other,
                null,
                null,
                ReturnFinancialTreatment.None,
                null,
                null,
                [CustomerItem(Guid.NewGuid(), 1, 0, 1)]));
    }

    private static ReturnRequest CustomerReturn(IReadOnlyList<ReturnItem> items)
        => ReturnRequest.CreateCustomer(
            "RET-CUSTOMER",
            Guid.NewGuid(),
            "Acme",
            Guid.NewGuid(),
            "ORD-1",
            ReturnReason.Defective,
            null,
            null,
            ReturnFinancialTreatment.CustomerRefund,
            "Pending refund",
            Guid.NewGuid(),
            items);

    private static ReturnRequest SupplierReturn(IReadOnlyList<ReturnItem> items)
        => ReturnRequest.CreateSupplier(
            "RET-SUPPLIER",
            Guid.NewGuid(),
            "Supplier",
            Guid.NewGuid(),
            "PO-1",
            ReturnReason.Damaged,
            null,
            null,
            ReturnFinancialTreatment.SupplierCredit,
            null,
            Guid.NewGuid(),
            items);

    private static ReturnItem CustomerItem(Guid orderItemId, int sourceQuantity, int previouslyReturned, int requested)
        => ReturnItem.CreateForCustomer(
            Guid.NewGuid(),
            "SKU-1",
            "Widget",
            orderItemId,
            sourceQuantity,
            previouslyReturned,
            requested,
            10m);

    private static ReturnItem SupplierItem(Guid purchaseItemId, int received, int previouslyReturned, int requested)
        => ReturnItem.CreateForSupplier(
            Guid.NewGuid(),
            "SKU-1",
            "Widget",
            purchaseItemId,
            received,
            previouslyReturned,
            requested,
            4m);

    private static Order DeliveredOrder(int quantity)
    {
        var order = Order.Create(
            "ORD-TEST",
            Guid.NewGuid(),
            "Customer",
            Domain.ValueObjects.OrderShippingAddress.Create(
                "00000-000",
                "Street",
                "100",
                null,
                null,
                "City",
                "ST",
                "BR"),
            [OrderItem.Create(Guid.NewGuid(), "SKU", "Name", quantity, 10m)]);
        order.Confirm();
        order.Ship();
        order.Deliver();
        return order;
    }

    private static PurchaseOrder ReceivedPurchase(int ordered, int received)
    {
        var order = PurchaseOrder.Create(
            "PO-TEST",
            Guid.NewGuid(),
            "Supplier",
            null,
            [PurchaseOrderItem.Create(Guid.NewGuid(), "SKU", "Name", ordered, 2m)]);
        order.Send();
        order.Receive([(order.Items.Single().Id, received)]);
        return order;
    }
}
