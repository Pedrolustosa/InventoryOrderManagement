using System.Text.Json.Serialization;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Orders.Dtos;

/// <summary>Chronological history of a sales order across order, stock, shipment, finance and returns.</summary>
public sealed class OrderTimelineDto
{
    public required Guid OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required OrderStatus CurrentStatus { get; init; }

    public required IReadOnlyList<OrderTimelineEntryDto> Entries { get; init; }
}

public enum OrderTimelineModule
{
    Order,
    Stock,
    Shipment,
    Finance,
    Return
}

/// <summary>
/// One timeline event. <see cref="EventType"/> and <see cref="Status"/> are stable codes for the client to
/// translate; no human-readable text is returned.
/// </summary>
public sealed class OrderTimelineEntryDto
{
    public required DateTime OccurredAtUtc { get; init; }

    public required OrderTimelineModule Module { get; init; }

    /// <summary>Stable event code, e.g. <c>order.confirmed</c> or <c>finance.settled</c>.</summary>
    public required string EventType { get; init; }

    /// <summary>Resulting status code of the referenced entity, when the event changes one.</summary>
    public string? Status { get; init; }

    public required OrderTimelineSummaryDto Summary { get; init; }

    /// <summary>Order, Shipment, Product, FinancialTitle or ReturnRequest.</summary>
    public required string ReferenceType { get; init; }

    public required Guid ReferenceId { get; init; }

    public PerformedByDto? PerformedBy { get; init; }
}

/// <summary>Structured event data. Only the fields that apply to the event are sent.</summary>
public sealed class OrderTimelineSummaryDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ProductId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductSku { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Quantity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? DueDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TitleNumber { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FinancialTitleType? TitleType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FinancialPaymentMethod? PaymentMethod { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReturnNumber { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReturnType? ReturnType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CarrierName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TrackingCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ShipmentId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ReturnRequestId { get; init; }
}

public sealed class PerformedByDto
{
    public required Guid UserId { get; init; }

    public string? DisplayName { get; init; }
}
