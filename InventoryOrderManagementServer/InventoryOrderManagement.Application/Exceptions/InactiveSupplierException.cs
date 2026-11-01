namespace InventoryOrderManagement.Application.Exceptions;

public class InactiveSupplierException : Exception
{
    public Guid SupplierId { get; }

    public InactiveSupplierException(Guid supplierId)
        : base($"Supplier '{supplierId}' is inactive and cannot be updated.")
    {
        SupplierId = supplierId;
    }
}
