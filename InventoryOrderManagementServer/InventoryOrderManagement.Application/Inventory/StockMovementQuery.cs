using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class StockMovementQuery
{
    public Guid? ProductId { get; init; }

    public Guid? SupplierId { get; init; }

    public StockMovementType? MovementType { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }
}
