namespace InventoryOrderManagement.Application.Notifications;

/// <summary>
/// Pure rules for when a stock change should emit a low-stock notification.
/// </summary>
public static class LowStockNotificationRules
{
    public static bool ShouldNotify(int previousQuantity, int newQuantity, int minimumStockLevel)
    {
        var crossedBelow = previousQuantity > minimumStockLevel && newQuantity <= minimumStockLevel;
        var depletedToZero = previousQuantity > 0 && newQuantity == 0;
        return crossedBelow || depletedToZero;
    }

    public static bool IsCritical(int newQuantity) => newQuantity == 0;
}
