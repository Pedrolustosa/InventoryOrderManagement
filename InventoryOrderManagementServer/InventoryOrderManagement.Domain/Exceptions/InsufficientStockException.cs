namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a stock issue would make the on-hand quantity negative.
/// Kept separate from <see cref="DomainException"/> so the API can map it to HTTP 409.
/// </summary>
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
