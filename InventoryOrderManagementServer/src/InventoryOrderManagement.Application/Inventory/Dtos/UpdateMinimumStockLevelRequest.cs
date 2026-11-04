namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class UpdateMinimumStockLevelRequest
{
    public required int MinimumStockLevel { get; init; }
}
