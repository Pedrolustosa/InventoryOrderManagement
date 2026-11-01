using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Application.Customers.Dtos;

public sealed class CustomerAddressDto
{
    public required string PostalCode { get; init; }

    public required string Street { get; init; }

    public required string Number { get; init; }

    public string? Complement { get; init; }

    public string? Neighborhood { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string Country { get; init; }

    public static CustomerAddressDto FromValueObject(CustomerAddress address)
    {
        return new CustomerAddressDto
        {
            PostalCode = address.PostalCode,
            Street = address.Street,
            Number = address.Number,
            Complement = address.Complement,
            Neighborhood = address.Neighborhood,
            City = address.City,
            State = address.State,
            Country = address.Country
        };
    }

    public CustomerAddress ToValueObject()
    {
        return CustomerAddress.Create(
            PostalCode,
            Street,
            Number,
            Complement,
            Neighborhood,
            City,
            State,
            Country);
    }
}
