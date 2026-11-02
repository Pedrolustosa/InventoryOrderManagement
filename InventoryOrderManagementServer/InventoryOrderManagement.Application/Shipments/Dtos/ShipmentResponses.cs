using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Shipments.Dtos;

public sealed class ShipmentStatusHistoryResponse
{
    public required Guid Id { get; init; }

    public ShipmentStatus? PreviousStatus { get; init; }

    public required ShipmentStatus NewStatus { get; init; }

    public string? Notes { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public Guid? ChangedByUserId { get; init; }

    public static ShipmentStatusHistoryResponse FromEntity(ShipmentStatusHistory entry) => new()
    {
        Id = entry.Id,
        PreviousStatus = entry.PreviousStatus,
        NewStatus = entry.NewStatus,
        Notes = entry.Notes,
        OccurredAtUtc = entry.OccurredAtUtc,
        ChangedByUserId = entry.ChangedByUserId
    };
}

public sealed class ShipmentResponse
{
    public required Guid Id { get; init; }

    public required Guid OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required ShipmentStatus Status { get; init; }

    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }

    public string? FailureReason { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? ShippedAtUtc { get; init; }

    public DateTime? DeliveredAtUtc { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public required int Version { get; init; }

    public OrderShippingAddressResponse? ShippingAddress { get; init; }

    public required IReadOnlyList<ShipmentStatusHistoryResponse> History { get; init; }

    public static ShipmentResponse FromEntity(Shipment entity, Order? order = null) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        OrderNumber = entity.OrderNumber,
        CustomerId = entity.CustomerId,
        CustomerName = entity.CustomerName,
        Status = entity.Status,
        CarrierName = entity.CarrierName,
        TrackingCode = entity.TrackingCode,
        Notes = entity.Notes,
        FailureReason = entity.FailureReason,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        ShippedAtUtc = entity.ShippedAtUtc,
        DeliveredAtUtc = entity.DeliveredAtUtc,
        CreatedByUserId = entity.CreatedByUserId,
        Version = entity.Version,
        ShippingAddress = order is null
            ? null
            : OrderShippingAddressResponse.FromAddress(order.ShippingAddress),
        History = entity.History
            .OrderBy(entry => entry.OccurredAtUtc)
            .Select(ShipmentStatusHistoryResponse.FromEntity)
            .ToList()
    };
}

public sealed class ShipmentSummaryResponse
{
    public required Guid Id { get; init; }

    public required Guid OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required ShipmentStatus Status { get; init; }

    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? ShippedAtUtc { get; init; }

    public DateTime? DeliveredAtUtc { get; init; }

    public static ShipmentSummaryResponse FromEntity(Shipment entity) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        OrderNumber = entity.OrderNumber,
        CustomerId = entity.CustomerId,
        CustomerName = entity.CustomerName,
        Status = entity.Status,
        CarrierName = entity.CarrierName,
        TrackingCode = entity.TrackingCode,
        CreatedAtUtc = entity.CreatedAtUtc,
        ShippedAtUtc = entity.ShippedAtUtc,
        DeliveredAtUtc = entity.DeliveredAtUtc
    };
}
