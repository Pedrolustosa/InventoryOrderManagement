using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Application.Orders.Dtos;

public sealed class OrderItemResponse
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public required decimal LineTotal { get; init; }

    public static OrderItemResponse FromItem(OrderItem item)
    {
        return new OrderItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductSku = item.ProductSku,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }
}

public sealed class OrderShippingAddressResponse
{
    public required string PostalCode { get; init; }

    public required string Street { get; init; }

    public required string Number { get; init; }

    public string? Complement { get; init; }

    public string? Neighborhood { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string Country { get; init; }

    public static OrderShippingAddressResponse FromAddress(OrderShippingAddress address)
    {
        return new OrderShippingAddressResponse
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
}

public sealed class OrderCreatedResponse
{
    public required Guid Id { get; init; }

    public required string OrderNumber { get; init; }

    public required OrderStatus Status { get; init; }

    public required decimal Subtotal { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static OrderCreatedResponse FromOrder(Order order)
    {
        return new OrderCreatedResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            Subtotal = order.Subtotal,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }
}

public sealed class OrderSummaryResponse
{
    public required Guid Id { get; init; }

    public required string OrderNumber { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required OrderStatus Status { get; init; }

    public required decimal Subtotal { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static OrderSummaryResponse FromOrder(Order order)
    {
        return new OrderSummaryResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Status = order.Status,
            Subtotal = order.Subtotal,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }
}

public sealed class OrderResponse
{
    public required Guid Id { get; init; }

    public required string OrderNumber { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required OrderStatus Status { get; init; }

    public required IReadOnlyList<OrderItemResponse> Items { get; init; }

    public required OrderShippingAddressResponse ShippingAddress { get; init; }

    public required decimal Subtotal { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? ConfirmedAtUtc { get; init; }

    public DateTime? ShippedAtUtc { get; init; }

    public DateTime? DeliveredAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public string? CancellationReason { get; init; }

    public static OrderResponse FromOrder(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Status = order.Status,
            Items = order.Items.Select(OrderItemResponse.FromItem).ToList(),
            ShippingAddress = OrderShippingAddressResponse.FromAddress(order.ShippingAddress),
            Subtotal = order.Subtotal,
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            ConfirmedAtUtc = order.ConfirmedAtUtc,
            ShippedAtUtc = order.ShippedAtUtc,
            DeliveredAtUtc = order.DeliveredAtUtc,
            CancelledAtUtc = order.CancelledAtUtc,
            CancellationReason = order.CancellationReason
        };
    }
}
