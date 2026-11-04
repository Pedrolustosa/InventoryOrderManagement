using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Suppliers.Dtos;

public sealed class SupplierResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? TaxIdentifier { get; init; }

    public string? ContactName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public static SupplierResponse FromEntity(Supplier supplier)
    {
        return new SupplierResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            TaxIdentifier = supplier.TaxIdentifier,
            ContactName = supplier.ContactName,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedAtUtc = supplier.CreatedAtUtc,
            UpdatedAtUtc = supplier.UpdatedAtUtc
        };
    }
}
