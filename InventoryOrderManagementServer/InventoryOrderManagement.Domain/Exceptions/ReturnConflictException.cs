namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a return operation conflicts with available quantities or current state.
/// The API maps it to HTTP 409.
/// </summary>
public sealed class ReturnConflictException : Exception
{
    public ReturnConflictException(string message)
        : base(message)
    {
    }
}
