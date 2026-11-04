using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Customers.Dtos;

public sealed class CustomerResponse
{
    public required Guid Id { get; init; }

    public required CustomerType CustomerType { get; init; }

    public required string Name { get; init; }

    public string? TaxIdentifier { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public CustomerAddressDto? Address { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public static CustomerResponse FromEntity(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            Name = customer.Name,
            TaxIdentifier = customer.TaxIdentifier,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address is null
                ? null
                : CustomerAddressDto.FromValueObject(customer.Address),
            IsActive = customer.IsActive,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
    }
}
