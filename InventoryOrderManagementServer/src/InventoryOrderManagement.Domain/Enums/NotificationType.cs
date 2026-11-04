namespace InventoryOrderManagement.Domain.Enums;

public enum NotificationType
{
    LowStockDetected = 0,
    OrderConfirmed = 1,
    ShipmentFailed = 2,
    ReturnAwaitingInspection = 3
}
