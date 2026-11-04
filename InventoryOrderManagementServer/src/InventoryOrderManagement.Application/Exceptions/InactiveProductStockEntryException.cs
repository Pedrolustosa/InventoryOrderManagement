namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InactiveProductStockEntryException : Exception
{
    public InactiveProductStockEntryException(Guid productId)
        : base($"Product '{productId}' is inactive and cannot receive stock.")
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
