namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class AdjustStockResult
{
    public required bool MovementCreated { get; init; }

    public required InventoryBalanceResponse Balance { get; init; }

    public StockMovementResponse? Movement { get; init; }
}
