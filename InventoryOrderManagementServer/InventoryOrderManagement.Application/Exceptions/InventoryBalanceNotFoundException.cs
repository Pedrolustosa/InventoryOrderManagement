namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InventoryBalanceNotFoundException : Exception
{
    public InventoryBalanceNotFoundException(Guid productId)
        : base($"Inventory balance for product '{productId}' was not found.")
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
