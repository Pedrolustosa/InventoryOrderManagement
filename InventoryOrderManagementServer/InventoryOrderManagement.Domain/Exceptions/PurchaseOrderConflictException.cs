namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a purchase operation breaks a business rule that is not a malformed request.
/// The API maps it to HTTP 409.
/// </summary>
public sealed class PurchaseOrderConflictException : Exception
{
    public PurchaseOrderConflictException(string message)
        : base(message)
    {
    }
}
