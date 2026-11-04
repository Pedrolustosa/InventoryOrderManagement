namespace InventoryOrderManagement.Application.Orders.Dtos;

public sealed class CancelOrderRequest
{
    public string Reason { get; init; } = string.Empty;
}
