using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class PurchaseOrderTests
{
    [Fact]
    public void Send_FromDraft_MovesToSent()
    {
        var order = Draft();

        order.Send();

        Assert.Equal(PurchaseOrderStatus.Sent, order.Status);
        Assert.NotNull(order.SentAtUtc);
        Assert.Equal(25.00m, order.Subtotal);
    }

    [Fact]
    public void ReplaceDraft_AfterSend_ThrowsInvalidStatus()
    {
        var order = Draft();
        order.Send();

        Assert.Throws<InvalidPurchaseOrderStatusException>(() =>
            order.ReplaceDraft(null, [Line(order.Items.Single().ProductId, 1, 1m)]));
    }

    [Fact]
    public void Receive_PartialThenComplete_ReachesReceived()
    {
        var order = Draft();
        order.Send();
        var itemId = order.Items.Single().Id;

        order.Receive([(itemId, 8)]);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);
        Assert.Null(order.ReceivedAtUtc);
        Assert.Equal(8, order.Items.Single().ReceivedQuantity);
        Assert.Equal(25.00m, order.Subtotal);

        order.Receive([(itemId, 12)]);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.NotNull(order.ReceivedAtUtc);
        Assert.Equal(20, order.Items.Single().ReceivedQuantity);
    }

    [Fact]
    public void Receive_AbovePending_ThrowsConflictAndKeepsQuantity()
    {
        var order = Draft();
        order.Send();
        var item = order.Items.Single();

        var exception = Assert.Throws<PurchaseOrderConflictException>(() => order.Receive([(item.Id, 21)]));

        Assert.Contains("pending", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, item.ReceivedQuantity);
        Assert.Equal(PurchaseOrderStatus.Sent, order.Status);
    }

    [Fact]
    public void Cancel_AfterPartialReceipt_KeepsReceivedQuantity()
    {
        var order = Draft();
        order.Send();
        var item = order.Items.Single();
        order.Receive([(item.Id, 8)]);

        order.Cancel("Supplier short");

        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
        Assert.Equal(8, item.ReceivedQuantity);
        Assert.Equal("Supplier short", order.CancellationReason);
    }

    [Fact]
    public void Cancel_WhenReceived_ThrowsInvalidStatus()
    {
        var order = Draft();
        order.Send();
        order.Receive([(order.Items.Single().Id, 20)]);

        Assert.Throws<InvalidPurchaseOrderStatusException>(() => order.Cancel("Too late"));
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
    }

    [Fact]
    public void Create_WithDuplicateProduct_ThrowsConflict()
    {
        var productId = Guid.NewGuid();

        var exception = Assert.Throws<PurchaseOrderConflictException>(() =>
            PurchaseOrder.Create(
                "PO-20261101-AAAAAAAA",
                Guid.NewGuid(),
                "Supplier",
                null,
                [Line(productId, 2, 1m), Line(productId, 3, 1m)]));

        Assert.Contains("same product", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNegativeCost_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Line(Guid.NewGuid(), 1, -0.01m));
    }

    [Fact]
    public void Create_RoundsLineTotalAwayFromZero()
    {
        var item = Line(Guid.NewGuid(), 3, 1.005m);

        Assert.Equal(1.01m, item.UnitCost);
        Assert.Equal(3.03m, item.LineTotal);
    }

    private static PurchaseOrder Draft()
    {
        return PurchaseOrder.Create(
            "PO-20261101-AAAAAAAA",
            Guid.NewGuid(),
            "Acme Supply",
            null,
            [Line(Guid.NewGuid(), 20, 1.25m)]);
    }

    private static PurchaseOrderItem Line(Guid productId, int quantity, decimal unitCost)
    {
        return PurchaseOrderItem.Create(productId, "SKU-1", "Widget", quantity, unitCost);
    }
}
