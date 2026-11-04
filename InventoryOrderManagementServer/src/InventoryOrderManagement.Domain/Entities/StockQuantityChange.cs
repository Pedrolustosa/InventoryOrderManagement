using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Entities;

public sealed record StockQuantityChange(
    StockMovementType MovementType,
    int Quantity,
    int PreviousQuantity,
    int NewQuantity);
