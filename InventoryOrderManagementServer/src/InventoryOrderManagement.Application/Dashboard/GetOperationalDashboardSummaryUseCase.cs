using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Dashboard.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Dashboard;

public sealed class GetOperationalDashboardSummaryUseCase
{
    private readonly IInventoryReportsQuery _inventoryReportsQuery;
    private readonly IOrderReportsQuery _orderReportsQuery;
    private readonly IDashboardQuery _dashboardQuery;
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public GetOperationalDashboardSummaryUseCase(
        IInventoryReportsQuery inventoryReportsQuery,
        IOrderReportsQuery orderReportsQuery,
        IDashboardQuery dashboardQuery,
        IUserNotificationRepository notificationRepository,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _inventoryReportsQuery = inventoryReportsQuery;
        _orderReportsQuery = orderReportsQuery;
        _dashboardQuery = dashboardQuery;
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<OperationalDashboardSummaryResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        CancellationToken cancellationToken = default)
    {
        var generatedAtUtc = _time.GetUtcNow().UtcDateTime;
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period, generatedAtUtc);
        var today = DateOnly.FromDateTime(generatedAtUtc);

        var stock = await _inventoryReportsQuery.GetStockCountsAsync(cancellationToken);
        var orderCounts = await _orderReportsQuery.GetStatusCountsAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var commercial = await _orderReportsQuery.GetCommercialOrdersAsync(
            customerId: null,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var (confirmedValue, averageConfirmed, confirmedInPeriod) = CommercialValue.Summarize(commercial);

        var parties = await _dashboardQuery.GetPartyCountsAsync(cancellationToken);
        var purchaseCounts = await _dashboardQuery.GetPurchaseStatusCountsAsync(cancellationToken);
        var purchaseCommercial = await _dashboardQuery.GetPurchaseCommercialAsync(
            window.StartUtc,
            window.EndUtc,
            cancellationToken);
        var purchaseValue = MoneyMath.Round(purchaseCommercial.Sum(item => item.Subtotal));
        var shipments = await _dashboardQuery.GetShipmentStatusCountsAsync(cancellationToken);
        var returns = await _dashboardQuery.GetReturnStatusCountsAsync(cancellationToken);

        DashboardFinanceSection? finance = null;
        if (_currentUser.Permissions.Contains(Permissions.Finance.ReportsView))
        {
            var metrics = await _dashboardQuery.GetFinanceOpenMetricsAsync(today, cancellationToken);
            finance = new DashboardFinanceSection
            {
                PayablesPending = metrics.PayablesPendingCount,
                ReceivablesPending = metrics.ReceivablesPendingCount,
                OverduePayables = metrics.OverduePayablesCount,
                OverdueReceivables = metrics.OverdueReceivablesCount,
                PayablesPendingAmount = metrics.PayablesPendingAmount,
                ReceivablesPendingAmount = metrics.ReceivablesPendingAmount,
                OverduePayablesAmount = metrics.OverduePayablesAmount,
                OverdueReceivablesAmount = metrics.OverdueReceivablesAmount
            };
        }

        DashboardNotificationsSection? notifications = null;
        if (_currentUser.UserId is Guid userId
            && _currentUser.Permissions.Contains(Permissions.Notifications.View))
        {
            var unread = await _notificationRepository.CountUnreadAsync(userId, cancellationToken);
            notifications = new DashboardNotificationsSection { Unread = unread };
        }

        var lowStockPositiveOnly = Math.Max(0, stock.LowStockProducts - stock.OutOfStockProducts);

        return new OperationalDashboardSummaryResponse
        {
            GeneratedAtUtc = generatedAtUtc,
            Products = new DashboardProductsSection
            {
                Total = stock.TotalProducts,
                Active = stock.ActiveProducts,
                Inactive = stock.InactiveProducts,
                LowStock = lowStockPositiveOnly,
                OutOfStock = stock.OutOfStockProducts,
                TotalUnitsInStock = stock.TotalUnitsInStock
            },
            Customers = new DashboardPartySection
            {
                Total = parties.CustomersTotal,
                Active = parties.CustomersActive
            },
            Suppliers = new DashboardPartySection
            {
                Total = parties.SuppliersTotal,
                Active = parties.SuppliersActive
            },
            Orders = new DashboardOrdersSection
            {
                Total = orderCounts.TotalOrdersCreated,
                Draft = orderCounts.DraftOrders,
                Confirmed = orderCounts.ConfirmedOrders,
                Shipped = orderCounts.ShippedOrders,
                Delivered = orderCounts.DeliveredOrders,
                Cancelled = orderCounts.CancelledOrders,
                ConfirmedOrderValue = confirmedValue,
                AverageConfirmedOrderValue = averageConfirmed,
                ConfirmedInPeriod = confirmedInPeriod
            },
            Purchases = new DashboardPurchasesSection
            {
                Total = purchaseCounts.Total,
                Draft = purchaseCounts.Draft,
                Pending = purchaseCounts.Sent + purchaseCounts.PartiallyReceived,
                Completed = purchaseCounts.Received,
                Cancelled = purchaseCounts.Cancelled,
                CommercialValue = purchaseValue,
                SentInPeriod = purchaseCommercial.Count
            },
            Finance = finance,
            Shipping = new DashboardShippingSection
            {
                Pending = shipments.Preparing,
                InTransit = shipments.Shipped + shipments.InTransit,
                Delivered = shipments.Delivered,
                Failed = shipments.Failed
            },
            Returns = new DashboardReturnsSection
            {
                Pending = returns.Draft + returns.Authorized + returns.Received + returns.Dispatched,
                AwaitingInspection = returns.UnderInspection,
                Completed = returns.Completed + returns.Rejected,
                Cancelled = returns.Cancelled
            },
            Notifications = notifications
        };
    }
}
