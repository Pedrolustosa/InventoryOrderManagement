namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class ReturnConflictException : Exception
{
    public ReturnConflictException(string message)
        : base(message)
    {
    }
}
