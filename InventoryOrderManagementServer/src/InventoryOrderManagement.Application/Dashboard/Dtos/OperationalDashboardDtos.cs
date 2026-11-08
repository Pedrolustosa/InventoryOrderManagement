using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Dashboard.Dtos;

public sealed class OperationalDashboardSummaryResponse
{
    public required DateTime GeneratedAtUtc { get; init; }

    public required DashboardProductsSection Products { get; init; }

    public required DashboardPartySection Customers { get; init; }

    public required DashboardPartySection Suppliers { get; init; }

    public required DashboardOrdersSection Orders { get; init; }

    public required DashboardPurchasesSection Purchases { get; init; }

    public DashboardFinanceSection? Finance { get; init; }

    public required DashboardShippingSection Shipping { get; init; }

    public required DashboardReturnsSection Returns { get; init; }

    public DashboardNotificationsSection? Notifications { get; init; }
}

public sealed class DashboardProductsSection
{
    public int Total { get; init; }

    public int Active { get; init; }

    public int Inactive { get; init; }

    public int LowStock { get; init; }

    public int OutOfStock { get; init; }

    public int TotalUnitsInStock { get; init; }
}

public sealed class DashboardPartySection
{
    public int Total { get; init; }

    public int Active { get; init; }
}

public sealed class DashboardOrdersSection
{
    public int Total { get; init; }

    public int Draft { get; init; }

    public int Confirmed { get; init; }

    public int Shipped { get; init; }

    public int Delivered { get; init; }

    public int Cancelled { get; init; }

    public decimal ConfirmedOrderValue { get; init; }

    public decimal AverageConfirmedOrderValue { get; init; }

    public int ConfirmedInPeriod { get; init; }
}

public sealed class DashboardPurchasesSection
{
    public int Total { get; init; }

    public int Draft { get; init; }

    public int Pending { get; init; }

    public int Completed { get; init; }

    public int Cancelled { get; init; }

    public decimal CommercialValue { get; init; }

    public int SentInPeriod { get; init; }
}

public sealed class DashboardFinanceSection
{
    public int PayablesPending { get; init; }

    public int ReceivablesPending { get; init; }

    public int OverduePayables { get; init; }

    public int OverdueReceivables { get; init; }

    public decimal PayablesPendingAmount { get; init; }

    public decimal ReceivablesPendingAmount { get; init; }

    public decimal OverduePayablesAmount { get; init; }

    public decimal OverdueReceivablesAmount { get; init; }
}

public sealed class DashboardShippingSection
{
    public int Pending { get; init; }

    public int InTransit { get; init; }

    public int Delivered { get; init; }

    public int Failed { get; init; }
}

public sealed class DashboardReturnsSection
{
    public int Pending { get; init; }

    public int AwaitingInspection { get; init; }

    public int Completed { get; init; }

    public int Cancelled { get; init; }
}

public sealed class DashboardNotificationsSection
{
    public int Unread { get; init; }
}

public sealed class DashboardTrendsResponse
{
    public required string GroupBy { get; init; }

    public required IReadOnlyList<DashboardTrendItemResponse> Items { get; init; }
}

public sealed class DashboardTrendItemResponse
{
    public required string Period { get; init; }

    public int SalesCount { get; init; }

    public decimal SalesAmount { get; init; }

    public int PurchasesCount { get; init; }

    public decimal PurchasesAmount { get; init; }
}

public sealed class DashboardRecentOrdersResponse
{
    public required IReadOnlyList<DashboardRecentOrderResponse> Items { get; init; }
}

public sealed class DashboardRecentOrderResponse
{
    public Guid OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required string CustomerName { get; init; }

    public required OrderStatus Status { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ConfirmedAtUtc { get; init; }
}
