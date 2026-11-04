namespace InventoryOrderManagement.Application.Exceptions;

public class SupplierNotFoundException : Exception
{
    public Guid SupplierId { get; }

    public SupplierNotFoundException(Guid supplierId)
        : base($"Supplier '{supplierId}' was not found.")
    {
        SupplierId = supplierId;
    }
}
