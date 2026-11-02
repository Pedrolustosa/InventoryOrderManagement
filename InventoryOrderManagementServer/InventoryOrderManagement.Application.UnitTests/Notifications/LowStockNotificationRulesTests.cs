using InventoryOrderManagement.Application.Notifications;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class LowStockNotificationRulesTests
{
    [Fact]
    public void ShouldNotify_WhenCrossingMinimum_IsTrue()
    {
        Assert.True(LowStockNotificationRules.ShouldNotify(previousQuantity: 15, newQuantity: 4, minimumStockLevel: 5));
        Assert.False(LowStockNotificationRules.IsCritical(4));
    }

    [Fact]
    public void ShouldNotify_WhenDepletingToZero_IsCritical()
    {
        Assert.True(LowStockNotificationRules.ShouldNotify(previousQuantity: 3, newQuantity: 0, minimumStockLevel: 5));
        Assert.True(LowStockNotificationRules.IsCritical(0));
    }

    [Fact]
    public void ShouldNotify_WhenStayingBelowMinimum_IsFalse()
    {
        Assert.False(LowStockNotificationRules.ShouldNotify(previousQuantity: 4, newQuantity: 3, minimumStockLevel: 5));
    }
}
