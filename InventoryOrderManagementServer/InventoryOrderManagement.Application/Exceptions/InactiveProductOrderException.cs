namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InactiveProductOrderException : Exception
{
    public InactiveProductOrderException(string productName)
        : base($"Product '{productName}' is inactive and cannot be included in or confirmed on an order.")
    {
        ProductName = productName;
    }

    public string ProductName { get; }
}
