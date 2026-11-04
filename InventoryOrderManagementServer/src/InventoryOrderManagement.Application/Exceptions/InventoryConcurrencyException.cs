namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InventoryConcurrencyException : Exception
{
    public InventoryConcurrencyException(Exception innerException)
        : base(
            "The stock balance was changed by another operation. Submit the request again.",
            innerException)
    {
    }
}
