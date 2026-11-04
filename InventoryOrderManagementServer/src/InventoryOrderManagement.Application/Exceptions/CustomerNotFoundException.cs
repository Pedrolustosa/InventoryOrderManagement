namespace InventoryOrderManagement.Application.Exceptions;

public class CustomerNotFoundException : Exception
{
    public Guid CustomerId { get; }

    public CustomerNotFoundException(Guid customerId)
        : base($"Customer '{customerId}' was not found.")
    {
        CustomerId = customerId;
    }
}
