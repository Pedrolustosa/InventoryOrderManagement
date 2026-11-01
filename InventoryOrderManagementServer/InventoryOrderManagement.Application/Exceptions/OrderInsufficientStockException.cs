namespace InventoryOrderManagement.Application.Exceptions;

public sealed class OrderInsufficientStockException : Exception
{
    public OrderInsufficientStockException(string productName, int availableQuantity, int requestedQuantity)
        : base($"Insufficient stock for '{productName}'. Available: {availableQuantity}. Requested: {requestedQuantity}.")
    {
        ProductName = productName;
        AvailableQuantity = availableQuantity;
        RequestedQuantity = requestedQuantity;
    }

    public string ProductName { get; }

    public int AvailableQuantity { get; }

    public int RequestedQuantity { get; }
}
