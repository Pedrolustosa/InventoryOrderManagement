namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class AdjustStockRequest
{
    public required Guid ProductId { get; init; }

    public required int NewQuantity { get; init; }

    public required string Reason { get; init; }
}
