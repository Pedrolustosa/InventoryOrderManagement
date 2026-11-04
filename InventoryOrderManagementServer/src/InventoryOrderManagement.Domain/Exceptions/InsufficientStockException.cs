namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException(int availableQuantity, int requestedQuantity)
        : base($"Insufficient stock. Available: {availableQuantity}. Requested: {requestedQuantity}.")
    {
        AvailableQuantity = availableQuantity;
        RequestedQuantity = requestedQuantity;
    }

    public int AvailableQuantity { get; }

    public int RequestedQuantity { get; }
}
