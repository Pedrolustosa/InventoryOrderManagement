namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InvalidOrderPeriodException : Exception
{
    public InvalidOrderPeriodException()
        : base("The start date cannot be later than the end date.")
    {
    }
}
