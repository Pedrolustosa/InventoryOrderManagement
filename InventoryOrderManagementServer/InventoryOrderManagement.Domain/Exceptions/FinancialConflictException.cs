namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a financial operation conflicts with the recorded history.
/// The API maps it to HTTP 409.
/// </summary>
public sealed class FinancialConflictException : Exception
{
    public FinancialConflictException(string message)
        : base(message)
    {
    }
}
