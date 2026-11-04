namespace InventoryOrderManagement.Application.Abstractions.Reports;

public interface IOrderReportsQuery
{
    Task<OrderStatusCounts> GetStatusCountsAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialOrderAmount>> GetCommercialOrdersAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<CancellationCohortCounts> GetCancellationCohortAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CancelledOrderAmount>> GetCancelledOrdersAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);
}

public sealed class OrderStatusCounts
{
    public int TotalOrdersCreated { get; init; }

    public int DraftOrders { get; init; }

    public int ConfirmedOrders { get; init; }

    public int ShippedOrders { get; init; }

    public int DeliveredOrders { get; init; }

    public int CancelledOrders { get; init; }
}

public sealed class CommercialOrderAmount
{
    public decimal Subtotal { get; init; }

    public DateTime ConfirmedAtUtc { get; init; }
}

public sealed class CancellationCohortCounts
{
    public int OrdersCreated { get; init; }

    public int CreatedAndCancelled { get; init; }
}

public sealed class CancelledOrderAmount
{
    public decimal Subtotal { get; init; }

    public DateTime CancelledAtUtc { get; init; }
}
