using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Events;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class InventoryBalanceLowStockEventTests
{
    [Fact]
    public void CrossingBelowMinimum_RaisesEventOnce_WithAvailableAndMinimum()
    {
        var balance = Balance(onHand: 10, minimum: 5);

        balance.Issue(6);

        var raised = Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(balance.DomainEvents));
        Assert.Equal(balance.ProductId, raised.ProductId);
        Assert.Equal(4, raised.Available);
        Assert.Equal(5, raised.MinimumStock);
        Assert.True(balance.IsLowStock);
    }

    [Fact]
    public void StayingBelowMinimum_DoesNotRaiseAgain()
    {
        var balance = Balance(onHand: 10, minimum: 5);
        balance.Issue(6);
        balance.ClearDomainEvents();

        balance.Issue(1);
        balance.Reserve(1);
        balance.Receive(1);

        Assert.Empty(balance.DomainEvents);
    }

    [Fact]
    public void ReturningAboveAndDroppingAgain_RaisesAgain()
    {
        var balance = Balance(onHand: 10, minimum: 5);
        balance.Issue(6);
        balance.ClearDomainEvents();

        balance.Receive(10);
        Assert.Empty(balance.DomainEvents);
        Assert.False(balance.IsLowStock);

        balance.Issue(11);

        Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(balance.DomainEvents));
    }

    [Fact]
    public void ReachingExactlyTheMinimum_IsNotBelow()
    {
        var balance = Balance(onHand: 10, minimum: 5);

        balance.Issue(5);

        Assert.Empty(balance.DomainEvents);
        Assert.False(balance.IsLowStock);
    }

    [Fact]
    public void MinimumZeroOrNotConfigured_NeverRaises()
    {
        var balance = Balance(onHand: 3, minimum: 0);

        balance.Issue(3);

        Assert.Empty(balance.DomainEvents);
        Assert.False(balance.IsLowStock);
    }

    [Fact]
    public void Reservation_ComparesAvailableNotOnHand()
    {
        var balance = Balance(onHand: 10, minimum: 5);

        balance.Reserve(6);

        Assert.Equal(10, balance.QuantityOnHand);
        var raised = Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(balance.DomainEvents));
        Assert.Equal(4, raised.Available);
    }

    [Fact]
    public void ConsumingAReservation_DoesNotChangeAvailable_AndDoesNotRaise()
    {
        var balance = Balance(onHand: 10, minimum: 5);
        balance.Reserve(6);
        balance.ClearDomainEvents();

        balance.ConsumeReservation(6);

        Assert.Equal(4, balance.AvailableQuantity);
        Assert.Empty(balance.DomainEvents);
    }

    [Fact]
    public void ManualAdjustmentAndRaisingTheMinimum_CanCrossTheMinimum()
    {
        var adjusted = Balance(onHand: 10, minimum: 5);
        adjusted.AdjustTo(2);
        Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(adjusted.DomainEvents));

        var raisedMinimum = Balance(onHand: 10, minimum: 5);
        raisedMinimum.SetMinimumStockLevel(12);
        Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(raisedMinimum.DomainEvents));
    }

    [Fact]
    public void ReleasingAReservation_RearmsTheAlert()
    {
        var balance = Balance(onHand: 10, minimum: 5);
        balance.Reserve(6);
        balance.Release(6);
        balance.ClearDomainEvents();

        balance.Reserve(7);

        Assert.IsType<StockBelowMinimumDomainEvent>(Assert.Single(balance.DomainEvents));
    }

    [Fact]
    public void EveryChange_KeepsBumpingTheConcurrencyVersion()
    {
        var balance = Balance(onHand: 10, minimum: 5);
        var version = balance.Version;

        balance.Reserve(1);
        balance.Release(1);
        balance.Issue(1);

        Assert.Equal(version + 3, balance.Version);
    }

    private static InventoryBalance Balance(int onHand, int minimum)
    {
        var balance = InventoryBalance.Create(Guid.NewGuid());
        if (onHand > 0)
        {
            balance.Receive(onHand);
        }

        balance.SetMinimumStockLevel(minimum);
        balance.ClearDomainEvents();
        return balance;
    }
}
