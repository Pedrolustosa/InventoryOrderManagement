namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class ReceiveStockRequest
{
    public required Guid ProductId { get; init; }

    public required Guid SupplierId { get; init; }

    public required int Quantity { get; init; }

    public string? Reason { get; init; }
}
