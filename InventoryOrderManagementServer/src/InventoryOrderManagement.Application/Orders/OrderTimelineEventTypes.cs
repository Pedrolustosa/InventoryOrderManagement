namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Stable event codes of the order timeline. They are part of the API contract: clients translate them, so
/// never rename one; add a new code instead.
/// </summary>
public static class OrderTimelineEventTypes
{
    public const string OrderCreated = "order.created";
    public const string OrderConfirmed = "order.confirmed";
    public const string OrderShipped = "order.shipped";
    public const string OrderDelivered = "order.delivered";
    public const string OrderCancelled = "order.cancelled";

    public const string StockReserved = "stock.reserved";
    public const string StockReservationReleased = "stock.reservation_released";
    public const string StockIssued = "stock.issued";
    public const string StockCancellationReturned = "stock.cancellation_returned";
    public const string StockReturnRestocked = "stock.return_restocked";
    public const string StockReturnQuarantined = "stock.return_quarantined";
    public const string StockReturnLost = "stock.return_lost";
    public const string StockExchangeIssued = "stock.exchange_issued";

    public const string ShipmentCreated = "shipment.created";
    public const string ShipmentDispatched = "shipment.dispatched";
    public const string ShipmentInTransit = "shipment.in_transit";
    public const string ShipmentFailed = "shipment.failed";
    public const string ShipmentResumed = "shipment.resumed";
    public const string ShipmentDelivered = "shipment.delivered";

    public const string FinanceReceivableCreated = "finance.receivable_created";
    public const string FinanceRefundCreated = "finance.refund_created";
    public const string FinanceDifferenceCreated = "finance.difference_created";
    public const string FinanceSettled = "finance.settled";
    public const string FinanceAbated = "finance.abated";
    public const string FinanceTitleCancelled = "finance.title_cancelled";

    public const string ReturnRequested = "return.requested";
    public const string ReturnReceived = "return.received";
    public const string ReturnCompleted = "return.completed";
    public const string ReturnRejected = "return.rejected";
    public const string ReturnCancelled = "return.cancelled";

    /// <summary>Logical flow order, used to break ties between events recorded at the same instant.</summary>
    public static readonly IReadOnlyList<string> FlowOrder =
    [
        OrderCreated,
        OrderConfirmed,
        StockReserved,
        ShipmentCreated,
        ShipmentDispatched,
        StockIssued,
        FinanceReceivableCreated,
        OrderShipped,
        ShipmentInTransit,
        ShipmentFailed,
        ShipmentResumed,
        ShipmentDelivered,
        OrderDelivered,
        FinanceSettled,
        ReturnRequested,
        ReturnReceived,
        ReturnCompleted,
        ReturnRejected,
        StockReturnRestocked,
        StockReturnQuarantined,
        StockReturnLost,
        StockExchangeIssued,
        FinanceAbated,
        FinanceRefundCreated,
        FinanceDifferenceCreated,
        FinanceTitleCancelled,
        ReturnCancelled,
        OrderCancelled,
        StockReservationReleased,
        StockCancellationReturned
    ];
}
