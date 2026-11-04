namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class PurchaseOrderConflictException : Exception
{
    public PurchaseOrderConflictException(string message)
        : base(message)
    {
    }
}
