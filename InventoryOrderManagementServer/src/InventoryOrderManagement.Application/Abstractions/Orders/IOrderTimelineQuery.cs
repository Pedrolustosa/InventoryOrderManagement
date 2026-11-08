using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Orders;

/// <summary>
/// Read model for the order timeline. Implementations load every source with a fixed number of no-tracking,
/// projected queries (never one query per entry). Finance and return sources are only loaded when requested.
/// </summary>
public interface IOrderTimelineQuery
{
    /// <returns>The sources, or <c>null</c> when the order does not exist.</returns>
    Task<OrderTimelineSources?> LoadAsync(
        Guid orderId,
        bool includeFinance,
        bool includeReturns,
        CancellationToken cancellationToken = default);
}

public sealed class OrderTimelineSources
{
    public required OrderTimelineOrderRow Order { get; init; }

    public IReadOnlyList<OrderTimelineShipmentHistoryRow> ShipmentHistory { get; init; } = [];

    public IReadOnlyList<OrderTimelineMovementRow> Movements { get; init; } = [];

    public IReadOnlyList<OrderTimelineReturnRow> Returns { get; init; } = [];

    public IReadOnlyList<OrderTimelineTitleRow> Titles { get; init; } = [];

    public IReadOnlyList<OrderTimelineSettlementRow> Settlements { get; init; } = [];

    /// <summary>Display names of the users referenced by the rows above.</summary>
    public IReadOnlyDictionary<Guid, string> UserNames { get; init; } = new Dictionary<Guid, string>();
}

public sealed record OrderTimelineOrderRow(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal Subtotal,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? CancelledAtUtc);

public sealed record OrderTimelineShipmentHistoryRow(
    Guid ShipmentId,
    Guid HistoryId,
    string? CarrierName,
    string? TrackingCode,
    ShipmentStatus? PreviousStatus,
    ShipmentStatus NewStatus,
    DateTime OccurredAtUtc,
    Guid? ChangedByUserId);

public sealed record OrderTimelineMovementRow(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    StockMovementType MovementType,
    int Quantity,
    Guid? ShipmentId,
    Guid? ReturnRequestId,
    DateTime CreatedAtUtc);

public sealed record OrderTimelineReturnRow(
    Guid Id,
    string ReturnNumber,
    ReturnType Type,
    ReturnStatus Status,
    decimal EstimatedAmount,
    DateTime CreatedAtUtc,
    DateTime? ReceivedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    Guid? CreatedByUserId);

public sealed record OrderTimelineTitleRow(
    Guid Id,
    string TitleNumber,
    FinancialTitleType Type,
    FinancialSourceType SourceType,
    Guid? SourceId,
    decimal TotalAmount,
    DateOnly? FirstDueDate,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc,
    Guid? CreatedByUserId);

public sealed record OrderTimelineSettlementRow(
    Guid Id,
    Guid FinancialTitleId,
    FinancialPaymentMethod PaymentMethod,
    decimal SettledPrincipalAmount,
    DateTime OccurredAtUtc,
    Guid? CreatedByUserId);
