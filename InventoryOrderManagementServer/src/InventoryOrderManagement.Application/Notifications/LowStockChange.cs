namespace InventoryOrderManagement.Application.Notifications;

public sealed record LowStockChange(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int PreviousQuantity,
    int NewQuantity,
    int MinimumStockLevel,
    Guid StockMovementId);
