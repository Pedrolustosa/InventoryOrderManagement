namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InvalidMovementPeriodException : Exception
{
    public InvalidMovementPeriodException()
        : base("The start date cannot be later than the end date.")
    {
    }
}
