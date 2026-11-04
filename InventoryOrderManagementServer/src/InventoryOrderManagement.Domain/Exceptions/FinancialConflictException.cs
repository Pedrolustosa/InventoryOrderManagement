namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class FinancialConflictException : Exception
{
    public FinancialConflictException(string message)
        : base(message)
    {
    }
}
