namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class RegisterStockExitRequest
{
    public required Guid ProductId { get; init; }

    public required int Quantity { get; init; }

    public required string Reason { get; init; }
}
