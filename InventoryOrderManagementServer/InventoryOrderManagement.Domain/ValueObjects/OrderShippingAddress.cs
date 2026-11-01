using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.ValueObjects;

/// <summary>
/// Immutable snapshot of the shipping address captured for one order.
/// Later changes to the customer address do not affect this copy.
/// </summary>
public sealed class OrderShippingAddress : IEquatable<OrderShippingAddress>
{
    public const int PostalCodeMaxLength = 20;
    public const int StreetMaxLength = 200;
    public const int NumberMaxLength = 30;
    public const int ComplementMaxLength = 100;
    public const int NeighborhoodMaxLength = 100;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 50;
    public const int CountryMaxLength = 100;

    private OrderShippingAddress()
    {
    }

    public string PostalCode { get; private set; } = null!;

    public string Street { get; private set; } = null!;

    public string Number { get; private set; } = null!;

    public string? Complement { get; private set; }

    public string? Neighborhood { get; private set; }

    public string City { get; private set; } = null!;

    public string State { get; private set; } = null!;

    public string Country { get; private set; } = null!;

    public static OrderShippingAddress Create(
        string postalCode,
        string street,
        string number,
        string? complement,
        string? neighborhood,
        string city,
        string state,
        string country)
    {
        return new OrderShippingAddress
        {
            PostalCode = RequireText(postalCode, PostalCodeMaxLength, "Postal code"),
            Street = RequireText(street, StreetMaxLength, "Street"),
            Number = RequireText(number, NumberMaxLength, "Number"),
            Complement = OptionalText(complement, ComplementMaxLength, "Complement"),
            Neighborhood = OptionalText(neighborhood, NeighborhoodMaxLength, "Neighborhood"),
            City = RequireText(city, CityMaxLength, "City"),
            State = RequireText(state, StateMaxLength, "State"),
            Country = RequireText(country, CountryMaxLength, "Country")
        };
    }

    public static OrderShippingAddress FromCustomerAddress(CustomerAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return Create(
            address.PostalCode,
            address.Street,
            address.Number,
            address.Complement,
            address.Neighborhood,
            address.City,
            address.State,
            address.Country);
    }

    public bool Equals(OrderShippingAddress? other)
    {
        if (other is null)
        {
            return false;
        }

        return PostalCode == other.PostalCode
            && Street == other.Street
            && Number == other.Number
            && Complement == other.Complement
            && Neighborhood == other.Neighborhood
            && City == other.City
            && State == other.State
            && Country == other.Country;
    }

    public override bool Equals(object? obj) => Equals(obj as OrderShippingAddress);

    public override int GetHashCode() =>
        HashCode.Combine(PostalCode, Street, Number, Complement, Neighborhood, City, State, Country);

    private static string RequireText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required when address is provided.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? OptionalText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
