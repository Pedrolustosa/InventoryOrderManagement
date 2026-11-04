namespace InventoryOrderManagement.Application.Exceptions;

public class InactiveCustomerException : Exception
{
    public Guid CustomerId { get; }

    public InactiveCustomerException(Guid customerId)
        : base($"Customer '{customerId}' is inactive and cannot be updated.")
    {
        CustomerId = customerId;
    }
}
