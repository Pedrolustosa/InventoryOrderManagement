namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InactiveCustomerOrderException : Exception
{
    public InactiveCustomerOrderException(Guid customerId)
        : base($"Customer '{customerId}' is inactive and cannot place or confirm orders.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }
}
