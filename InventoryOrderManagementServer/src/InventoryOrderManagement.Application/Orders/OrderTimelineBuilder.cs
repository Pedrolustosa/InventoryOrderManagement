using InventoryOrderManagement.Application.Abstractions.Orders;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Enums;
using static InventoryOrderManagement.Application.Orders.OrderTimelineEventTypes;

namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Turns the loaded sources into timeline entries (pure: no I/O), ordered by time and, on ties, by the logical
/// flow in <see cref="OrderTimelineEventTypes.FlowOrder"/>.
/// </summary>
internal static class OrderTimelineBuilder
{
    private static readonly Dictionary<string, int> FlowRank = OrderTimelineEventTypes.FlowOrder
        .Select((eventType, index) => (eventType, index))
        .ToDictionary(pair => pair.eventType, pair => pair.index);

    public static OrderTimelineDto Build(OrderTimelineSources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var entries = new List<OrderTimelineEntryDto>();
        entries.AddRange(OrderEntries(sources.Order));
        entries.AddRange(sources.Movements.Select(StockEntry).OfType<OrderTimelineEntryDto>());
        entries.AddRange(sources.ShipmentHistory.Select(row => ShipmentEntry(row, sources.UserNames)));
        entries.AddRange(FinanceEntries(sources));
        entries.AddRange(sources.Returns.SelectMany(row => ReturnEntries(row, sources.UserNames)));

        return new OrderTimelineDto
        {
            OrderId = sources.Order.Id,
            OrderNumber = sources.Order.OrderNumber,
            CurrentStatus = sources.Order.Status,
            Entries = entries
                .Select(AsUtc)
                .OrderBy(entry => entry.OccurredAtUtc)
                .ThenBy(entry => FlowRank.GetValueOrDefault(entry.EventType, int.MaxValue))
                .ThenBy(entry => entry.ReferenceId)
                .ToList()
        };
    }

    /// <summary>
    /// Timestamps are stored in UTC but come back from the database without a kind; mark them as UTC so the
    /// contract serializes them with the "Z" suffix.
    /// </summary>
    private static OrderTimelineEntryDto AsUtc(OrderTimelineEntryDto entry) =>
        entry.OccurredAtUtc.Kind == DateTimeKind.Utc
            ? entry
            : new OrderTimelineEntryDto
            {
                OccurredAtUtc = DateTime.SpecifyKind(entry.OccurredAtUtc, DateTimeKind.Utc),
                Module = entry.Module,
                EventType = entry.EventType,
                Status = entry.Status,
                Summary = entry.Summary,
                ReferenceType = entry.ReferenceType,
                ReferenceId = entry.ReferenceId,
                PerformedBy = entry.PerformedBy
            };

    private static IEnumerable<OrderTimelineEntryDto> OrderEntries(OrderTimelineOrderRow order)
    {
        var summary = new OrderTimelineSummaryDto { Amount = order.Subtotal };

        yield return OrderEntry(order, order.CreatedAtUtc, OrderCreated, OrderStatus.Draft, summary);

        if (order.ConfirmedAtUtc is { } confirmed)
        {
            yield return OrderEntry(order, confirmed, OrderConfirmed, OrderStatus.Confirmed, summary);
        }

        if (order.ShippedAtUtc is { } shipped)
        {
            yield return OrderEntry(order, shipped, OrderShipped, OrderStatus.Shipped, summary);
        }

        if (order.DeliveredAtUtc is { } delivered)
        {
            yield return OrderEntry(order, delivered, OrderDelivered, OrderStatus.Delivered, summary);
        }

        if (order.CancelledAtUtc is { } cancelled)
        {
            yield return OrderEntry(order, cancelled, OrderCancelled, OrderStatus.Cancelled, summary);
        }
    }

    private static OrderTimelineEntryDto OrderEntry(
        OrderTimelineOrderRow order,
        DateTime occurredAtUtc,
        string eventType,
        OrderStatus status,
        OrderTimelineSummaryDto summary) =>
        new()
        {
            OccurredAtUtc = occurredAtUtc,
            Module = OrderTimelineModule.Order,
            EventType = eventType,
            Status = status.ToString(),
            Summary = summary,
            ReferenceType = "Order",
            ReferenceId = order.Id
        };

    private static OrderTimelineEntryDto? StockEntry(OrderTimelineMovementRow row)
    {
        var eventType = row.MovementType switch
        {
            StockMovementType.OrderReservation => StockReserved,
            StockMovementType.OrderReservationRelease => StockReservationReleased,
            StockMovementType.OrderExit => StockIssued,
            StockMovementType.OrderCancellationReturn => StockCancellationReturned,
            StockMovementType.CustomerReturnEntry => StockReturnRestocked,
            StockMovementType.CustomerReturnQuarantine => StockReturnQuarantined,
            StockMovementType.CustomerReturnLoss => StockReturnLost,
            StockMovementType.ExchangeReplacementExit => StockExchangeIssued,
            _ => null
        };

        if (eventType is null)
        {
            return null;
        }

        return new OrderTimelineEntryDto
        {
            OccurredAtUtc = row.CreatedAtUtc,
            Module = OrderTimelineModule.Stock,
            EventType = eventType,
            Summary = new OrderTimelineSummaryDto
            {
                ProductId = row.ProductId,
                ProductSku = row.ProductSku,
                ProductName = row.ProductName,
                Quantity = row.Quantity,
                ShipmentId = row.ShipmentId,
                ReturnRequestId = row.ReturnRequestId
            },
            ReferenceType = "Product",
            ReferenceId = row.ProductId
        };
    }

    private static OrderTimelineEntryDto ShipmentEntry(
        OrderTimelineShipmentHistoryRow row,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        var eventType = row.NewStatus switch
        {
            ShipmentStatus.Preparing => ShipmentCreated,
            ShipmentStatus.Shipped => ShipmentDispatched,
            ShipmentStatus.InTransit when row.PreviousStatus == ShipmentStatus.Failed => ShipmentResumed,
            ShipmentStatus.InTransit => ShipmentInTransit,
            ShipmentStatus.Failed => ShipmentFailed,
            ShipmentStatus.Delivered => ShipmentDelivered,
            _ => ShipmentCreated
        };

        return new OrderTimelineEntryDto
        {
            OccurredAtUtc = row.OccurredAtUtc,
            Module = OrderTimelineModule.Shipment,
            EventType = eventType,
            Status = row.NewStatus.ToString(),
            Summary = new OrderTimelineSummaryDto
            {
                CarrierName = row.CarrierName,
                TrackingCode = row.TrackingCode
            },
            ReferenceType = "Shipment",
            ReferenceId = row.ShipmentId,
            PerformedBy = Performer(row.ChangedByUserId, userNames)
        };
    }

    private static IEnumerable<OrderTimelineEntryDto> FinanceEntries(OrderTimelineSources sources)
    {
        var settlementsByTitle = sources.Settlements.ToLookup(settlement => settlement.FinancialTitleId);

        foreach (var title in sources.Titles)
        {
            var summary = TitleSummary(title);
            var createdEvent = title.SourceType == FinancialSourceType.Return
                ? title.Type == FinancialTitleType.Payable ? FinanceRefundCreated : FinanceDifferenceCreated
                : FinanceReceivableCreated;

            yield return FinanceEntry(
                title,
                title.CreatedAtUtc,
                createdEvent,
                FinancialTitleStatus.Pending,
                summary,
                Performer(title.CreatedByUserId, sources.UserNames));

            var settled = 0m;
            foreach (var settlement in settlementsByTitle[title.Id].OrderBy(item => item.OccurredAtUtc))
            {
                settled += settlement.SettledPrincipalAmount;
                yield return FinanceEntry(
                    title,
                    settlement.OccurredAtUtc,
                    settlement.PaymentMethod == FinancialPaymentMethod.ReturnCredit ? FinanceAbated : FinanceSettled,
                    settled >= title.TotalAmount ? FinancialTitleStatus.Paid : FinancialTitleStatus.PartiallyPaid,
                    new OrderTimelineSummaryDto
                    {
                        TitleNumber = title.TitleNumber,
                        TitleType = title.Type,
                        Amount = settlement.SettledPrincipalAmount,
                        PaymentMethod = settlement.PaymentMethod,
                        ReturnRequestId = summary.ReturnRequestId
                    },
                    Performer(settlement.CreatedByUserId, sources.UserNames));
            }

            if (title.CancelledAtUtc is { } cancelled)
            {
                yield return FinanceEntry(
                    title,
                    cancelled,
                    FinanceTitleCancelled,
                    FinancialTitleStatus.Cancelled,
                    summary,
                    performedBy: null);
            }
        }
    }

    private static OrderTimelineSummaryDto TitleSummary(OrderTimelineTitleRow title) =>
        new()
        {
            TitleNumber = title.TitleNumber,
            TitleType = title.Type,
            Amount = title.TotalAmount,
            DueDate = title.FirstDueDate,
            ReturnRequestId = title.SourceType == FinancialSourceType.Return ? title.SourceId : null
        };

    private static OrderTimelineEntryDto FinanceEntry(
        OrderTimelineTitleRow title,
        DateTime occurredAtUtc,
        string eventType,
        FinancialTitleStatus status,
        OrderTimelineSummaryDto summary,
        PerformedByDto? performedBy) =>
        new()
        {
            OccurredAtUtc = occurredAtUtc,
            Module = OrderTimelineModule.Finance,
            EventType = eventType,
            Status = status.ToString(),
            Summary = summary,
            ReferenceType = "FinancialTitle",
            ReferenceId = title.Id,
            PerformedBy = performedBy
        };

    private static IEnumerable<OrderTimelineEntryDto> ReturnEntries(
        OrderTimelineReturnRow row,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        var summary = new OrderTimelineSummaryDto
        {
            ReturnNumber = row.ReturnNumber,
            ReturnType = row.Type,
            Amount = row.EstimatedAmount
        };

        yield return ReturnEntry(row, row.CreatedAtUtc, ReturnRequested, ReturnStatus.Draft, summary, Performer(row.CreatedByUserId, userNames));

        if (row.ReceivedAtUtc is { } received)
        {
            yield return ReturnEntry(row, received, ReturnReceived, ReturnStatus.Received, summary, null);
        }

        if (row.CompletedAtUtc is { } completed)
        {
            var rejected = row.Status == ReturnStatus.Rejected;
            yield return ReturnEntry(
                row,
                completed,
                rejected ? ReturnRejected : ReturnCompleted,
                rejected ? ReturnStatus.Rejected : ReturnStatus.Completed,
                summary,
                null);
        }

        if (row.CancelledAtUtc is { } cancelled)
        {
            yield return ReturnEntry(row, cancelled, ReturnCancelled, ReturnStatus.Cancelled, summary, null);
        }
    }

    private static OrderTimelineEntryDto ReturnEntry(
        OrderTimelineReturnRow row,
        DateTime occurredAtUtc,
        string eventType,
        ReturnStatus status,
        OrderTimelineSummaryDto summary,
        PerformedByDto? performedBy) =>
        new()
        {
            OccurredAtUtc = occurredAtUtc,
            Module = OrderTimelineModule.Return,
            EventType = eventType,
            Status = status.ToString(),
            Summary = summary,
            ReferenceType = "ReturnRequest",
            ReferenceId = row.Id,
            PerformedBy = performedBy
        };

    private static PerformedByDto? Performer(Guid? userId, IReadOnlyDictionary<Guid, string> userNames) =>
        userId is { } id
            ? new PerformedByDto { UserId = id, DisplayName = userNames.GetValueOrDefault(id) }
            : null;
}
