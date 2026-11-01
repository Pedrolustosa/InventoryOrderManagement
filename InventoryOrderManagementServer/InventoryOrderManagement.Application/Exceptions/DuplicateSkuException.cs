namespace InventoryOrderManagement.Application.Exceptions;

/// <summary>
/// Raised when a create operation would violate SKU uniqueness.
/// Mapped to HTTP 409 by the API layer.
/// </summary>
public class DuplicateSkuException : Exception
{
    public string Sku { get; }

    public DuplicateSkuException(string sku)
        : base($"A product with SKU '{sku}' already exists.")
    {
        Sku = sku;
    }
}
