namespace InventoryOrderManagement.Application.Orders.Dtos;

public sealed class UpdateDraftOrderRequest
{
    public Guid CustomerId { get; init; }

    public OrderShippingAddressRequest? ShippingAddress { get; init; }

    public IReadOnlyList<OrderItemRequest> Items { get; init; } = [];
}
