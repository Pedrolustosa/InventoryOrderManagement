namespace InventoryOrderManagement.Application.Exceptions;

public sealed class ReturnNotFoundException : Exception
{
    public ReturnNotFoundException(Guid returnRequestId)
        : base($"Return '{returnRequestId}' was not found.")
    {
        ReturnRequestId = returnRequestId;
    }

    public Guid ReturnRequestId { get; }
}

public sealed class InvalidReturnPeriodException : Exception
{
    public InvalidReturnPeriodException()
        : base("The start date must be earlier than or equal to the end date.")
    {
    }
}
