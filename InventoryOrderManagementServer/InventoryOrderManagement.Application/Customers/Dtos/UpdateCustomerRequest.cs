using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Customers.Dtos;

public sealed class UpdateCustomerRequest
{
    public required CustomerType CustomerType { get; init; }

    public required string Name { get; init; }

    public string? TaxIdentifier { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public CustomerAddressDto? Address { get; init; }
}
