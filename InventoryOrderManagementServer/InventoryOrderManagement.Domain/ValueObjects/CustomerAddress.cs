using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a customer mailing address.
/// </summary>
public sealed class CustomerAddress : IEquatable<CustomerAddress>
{
    public const int PostalCodeMaxLength = 20;
    public const int StreetMaxLength = 200;
    public const int NumberMaxLength = 30;
    public const int ComplementMaxLength = 100;
    public const int NeighborhoodMaxLength = 100;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 50;
    public const int CountryMaxLength = 100;

    private CustomerAddress()
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

    public static CustomerAddress Create(
        string postalCode,
        string street,
        string number,
        string? complement,
        string? neighborhood,
        string city,
        string state,
        string country)
    {
        return new CustomerAddress
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

    public bool Equals(CustomerAddress? other)
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

    public override bool Equals(object? obj) => Equals(obj as CustomerAddress);

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
