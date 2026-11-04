using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class ShipmentTests
{
    [Fact]
    public void Create_StartsPreparing_WithHistory()
    {
        var shipment = CreateShipment(carrierName: "Acme Logistics");

        Assert.Equal(ShipmentStatus.Preparing, shipment.Status);
        Assert.Single(shipment.History);
        Assert.Null(shipment.History.Single().PreviousStatus);
        Assert.Equal(ShipmentStatus.Preparing, shipment.History.Single().NewStatus);
    }

    [Fact]
    public void Dispatch_RequiresCarrier_AndSetsShippedAt()
    {
        var shipment = CreateShipment(carrierName: null);

        Assert.Throws<DomainException>(() => shipment.Dispatch(null, null));

        shipment.UpdateInformation("Acme Logistics", "TRACK-1", null);
        shipment.Dispatch("Left warehouse", Guid.NewGuid());

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.NotNull(shipment.ShippedAtUtc);
        Assert.Equal(2, shipment.History.Count);
    }

    [Fact]
    public void HappyPath_Shipped_InTransit_Delivered()
    {
        var shipment = CreateReadyToDispatch();
        shipment.Dispatch(null, null);
        shipment.SetInTransit("On the road", null);
        shipment.MarkAsDelivered("Signed", null);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.NotNull(shipment.DeliveredAtUtc);
        Assert.Throws<InvalidShipmentStatusException>(() => shipment.SetInTransit(null, null));
    }

    [Fact]
    public void CanDeliverDirectlyFromShipped()
    {
        var shipment = CreateReadyToDispatch();
        shipment.Dispatch(null, null);
        shipment.MarkAsDelivered(null, null);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    [Fact]
    public void Failed_RequiresReason_AndCanResume()
    {
        var shipment = CreateReadyToDispatch();
        shipment.Dispatch(null, null);

        Assert.Throws<DomainException>(() => shipment.MarkAsFailed(" ", null, null));

        shipment.MarkAsFailed("Customer absent", "Attempt 1", null);
        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Customer absent", shipment.FailureReason);

        shipment.ResumeDelivery("Retry scheduled", null);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Null(shipment.FailureReason);
    }

    [Fact]
    public void UpdateInformation_OnlyWhilePreparing()
    {
        var shipment = CreateReadyToDispatch();
        shipment.Dispatch(null, null);

        Assert.Throws<InvalidShipmentStatusException>(() =>
            shipment.UpdateInformation("Other", null, null));
    }

    private static Shipment CreateReadyToDispatch()
        => CreateShipment(carrierName: "Acme Logistics", trackingCode: "TRK-9");

    private static Shipment CreateShipment(string? carrierName, string? trackingCode = null)
        => Shipment.Create(
            Guid.NewGuid(),
            "ORD-1",
            Guid.NewGuid(),
            "Jane Doe",
            carrierName,
            trackingCode,
            null,
            Guid.NewGuid());
}
