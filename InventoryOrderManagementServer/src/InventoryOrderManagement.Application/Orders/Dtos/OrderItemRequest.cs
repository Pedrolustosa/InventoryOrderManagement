namespace InventoryOrderManagement.Application.Orders.Dtos;

public sealed class OrderItemRequest
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }
}
