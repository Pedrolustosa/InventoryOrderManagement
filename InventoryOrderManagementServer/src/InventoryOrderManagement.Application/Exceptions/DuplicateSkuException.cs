namespace InventoryOrderManagement.Application.Exceptions;

public class DuplicateSkuException : Exception
{
    public string Sku { get; }

    public DuplicateSkuException(string sku)
        : base($"A product with SKU '{sku}' already exists.")
    {
        Sku = sku;
    }
}
