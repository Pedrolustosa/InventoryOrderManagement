namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InactiveSupplierStockEntryException : Exception
{
    public InactiveSupplierStockEntryException(Guid supplierId)
        : base($"Supplier '{supplierId}' is inactive and cannot be used to receive stock.")
    {
        SupplierId = supplierId;
    }

    public Guid SupplierId { get; }
}
