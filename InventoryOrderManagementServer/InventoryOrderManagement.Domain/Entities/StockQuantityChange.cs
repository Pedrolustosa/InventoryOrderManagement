using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Describes a quantity change already applied to an <see cref="InventoryBalance"/>.
/// The application uses it to record an immutable <see cref="StockMovement"/>.
/// </summary>
public sealed record StockQuantityChange(
    StockMovementType MovementType,
    int Quantity,
    int PreviousQuantity,
    int NewQuantity);
