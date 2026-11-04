namespace InventoryOrderManagement.Application.Notifications;

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
