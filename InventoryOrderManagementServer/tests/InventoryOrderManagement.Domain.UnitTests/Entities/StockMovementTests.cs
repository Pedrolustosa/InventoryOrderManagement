using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class StockMovementTests
{
    [Fact]
    public void CreateForShipment_LinksOrderAndShipment_AsOrderExit()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        var movement = StockMovement.CreateForShipment(productId, orderId, shipmentId, 3, 10, 7, "Dispatched.");

        Assert.Equal(StockMovementType.OrderExit, movement.MovementType);
        Assert.Equal(productId, movement.ProductId);
        Assert.Equal(orderId, movement.OrderId);
        Assert.Equal(shipmentId, movement.ShipmentId);
        Assert.Equal(3, movement.Quantity);
        Assert.Equal(10, movement.PreviousQuantity);
        Assert.Equal(7, movement.NewQuantity);
    }

    [Fact]
    public void CreateForShipment_WithoutShipment_Throws()
    {
        Assert.Throws<DomainException>(() =>
            StockMovement.CreateForShipment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 3, 10, 7, "Dispatched."));
    }

    [Fact]
    public void CreateForShipment_WithMismatchedResult_Throws()
    {
        Assert.Throws<DomainException>(() =>
            StockMovement.CreateForShipment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, 10, 8, "Dispatched."));
    }
}
