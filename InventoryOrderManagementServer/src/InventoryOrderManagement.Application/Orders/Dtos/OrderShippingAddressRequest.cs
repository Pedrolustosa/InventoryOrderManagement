namespace InventoryOrderManagement.Application.Orders.Dtos;

public sealed class OrderShippingAddressRequest
{
    public string PostalCode { get; init; } = string.Empty;

    public string Street { get; init; } = string.Empty;

    public string Number { get; init; } = string.Empty;

    public string? Complement { get; init; }

    public string? Neighborhood { get; init; }

    public string City { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}
