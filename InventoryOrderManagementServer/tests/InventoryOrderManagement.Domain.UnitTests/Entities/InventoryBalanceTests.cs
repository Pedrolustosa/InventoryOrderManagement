using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class InventoryBalanceTests
{
    [Fact]
    public void Reserve_WithSufficientAvailable_IncreasesReserved()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(10);

        balance.Reserve(4);

        Assert.Equal(10, balance.QuantityOnHand);
        Assert.Equal(4, balance.ReservedQuantity);
        Assert.Equal(6, balance.AvailableQuantity);
    }

    [Fact]
    public void Reserve_WithExactAvailable_Succeeds()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(5);

        balance.Reserve(5);

        Assert.Equal(5, balance.ReservedQuantity);
        Assert.Equal(0, balance.AvailableQuantity);
    }

    [Fact]
    public void Reserve_WithInsufficientAvailable_ThrowsInsufficientStockException()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(3);
        balance.Reserve(2);

        var exception = Assert.Throws<InsufficientStockException>(() => balance.Reserve(2));

        Assert.Equal(1, exception.AvailableQuantity);
        Assert.Equal(2, exception.RequestedQuantity);
        Assert.Equal(2, balance.ReservedQuantity);
    }

    [Fact]
    public void Release_WithReservedQuantity_DecreasesReserved()
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        balance.Receive(8);
        balance.Reserve(5);

        balance.Release(3);

        Assert.Equal(2, balance.ReservedQuantity);
        Assert.Equal(6, balance.AvailableQuantity);
    }
}
