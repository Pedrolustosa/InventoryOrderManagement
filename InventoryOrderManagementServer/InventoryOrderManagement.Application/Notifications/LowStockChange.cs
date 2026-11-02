namespace InventoryOrderManagement.Application.Notifications;

/// <summary>
/// Captured stock change used to evaluate low-stock notifications after a successful UoW.
/// </summary>
public sealed record LowStockChange(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int PreviousQuantity,
    int NewQuantity,
    int MinimumStockLevel,
    Guid StockMovementId);
