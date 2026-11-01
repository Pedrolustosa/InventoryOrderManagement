namespace InventoryOrderManagement.Domain.Enums;

public enum StockMovementType
{
    Entry = 0,
    Exit = 1,
    AdjustmentIncrease = 2,
    AdjustmentDecrease = 3,
    OrderExit = 4,
    OrderCancellationReturn = 5
}
