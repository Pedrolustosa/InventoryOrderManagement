namespace InventoryOrderManagement.Application.Suppliers.Dtos;

public sealed class UpdateSupplierRequest
{
    public required string Name { get; init; }

    public string? TaxIdentifier { get; init; }

    public string? ContactName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }
}
