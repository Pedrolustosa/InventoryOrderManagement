using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Dashboard;

public interface IDashboardQuery
{
    Task<DashboardPartyCounts> GetPartyCountsAsync(CancellationToken cancellationToken = default);

    Task<DashboardPurchaseStatusCounts> GetPurchaseStatusCountsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardPurchaseCommercialAmount>> GetPurchaseCommercialAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<DashboardShipmentStatusCounts> GetShipmentStatusCountsAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardReturnStatusCounts> GetReturnStatusCountsAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardFinanceOpenMetrics> GetFinanceOpenMetricsAsync(
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardSalesTrendPoint>> GetSalesTrendPointsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardPurchaseTrendPoint>> GetPurchaseTrendPointsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardRecentOrderRow>> GetRecentOrdersAsync(
        int take,
        CancellationToken cancellationToken = default);
}

public sealed class DashboardPartyCounts
{
    public int CustomersTotal { get; init; }

    public int CustomersActive { get; init; }

    public int SuppliersTotal { get; init; }

    public int SuppliersActive { get; init; }
}

public sealed class DashboardPurchaseStatusCounts
{
    public int Total { get; init; }

    public int Draft { get; init; }

    public int Sent { get; init; }

    public int PartiallyReceived { get; init; }

    public int Received { get; init; }

    public int Cancelled { get; init; }
}

public sealed class DashboardPurchaseCommercialAmount
{
    public decimal Subtotal { get; init; }

    public DateTime SentAtUtc { get; init; }
}

public sealed class DashboardShipmentStatusCounts
{
    public int Preparing { get; init; }

    public int Shipped { get; init; }

    public int InTransit { get; init; }

    public int Delivered { get; init; }

    public int Failed { get; init; }
}

public sealed class DashboardReturnStatusCounts
{
    public int Draft { get; init; }

    public int Authorized { get; init; }

    public int Received { get; init; }

    public int UnderInspection { get; init; }

    public int Completed { get; init; }

    public int Dispatched { get; init; }

    public int Rejected { get; init; }

    public int Cancelled { get; init; }
}

public sealed class DashboardFinanceOpenMetrics
{
    public int PayablesPendingCount { get; init; }

    public int ReceivablesPendingCount { get; init; }

    public int OverduePayablesCount { get; init; }

    public int OverdueReceivablesCount { get; init; }

    public decimal PayablesPendingAmount { get; init; }

    public decimal ReceivablesPendingAmount { get; init; }

    public decimal OverduePayablesAmount { get; init; }

    public decimal OverdueReceivablesAmount { get; init; }
}

public sealed class DashboardSalesTrendPoint
{
    public decimal Subtotal { get; init; }

    public DateTime ConfirmedAtUtc { get; init; }
}

public sealed class DashboardPurchaseTrendPoint
{
    public decimal Subtotal { get; init; }

    public DateTime SentAtUtc { get; init; }
}

public sealed class DashboardRecentOrderRow
{
    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = null!;

    public string CustomerName { get; init; } = null!;

    public OrderStatus Status { get; init; }

    public decimal Subtotal { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ConfirmedAtUtc { get; init; }
}
