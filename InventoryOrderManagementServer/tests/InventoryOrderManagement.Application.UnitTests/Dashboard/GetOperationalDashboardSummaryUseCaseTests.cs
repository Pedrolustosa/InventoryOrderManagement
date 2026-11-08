using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Application.Dashboard;
using InventoryOrderManagement.Application.Security;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Dashboard;

public class GetOperationalDashboardSummaryUseCaseTests
{
    private readonly IInventoryReportsQuery _inventory = Substitute.For<IInventoryReportsQuery>();
    private readonly IOrderReportsQuery _orders = Substitute.For<IOrderReportsQuery>();
    private readonly IDashboardQuery _dashboard = Substitute.For<IDashboardQuery>();
    private readonly IUserNotificationRepository _notifications = Substitute.For<IUserNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TimeProvider _time = TimeProvider.System;

    public GetOperationalDashboardSummaryUseCaseTests()
    {
        _inventory.GetStockCountsAsync(Arg.Any<CancellationToken>()).Returns(new InventoryStockCounts
        {
            TotalProducts = 10,
            ActiveProducts = 8,
            InactiveProducts = 2,
            TotalUnitsInStock = 100,
            LowStockProducts = 5,
            OutOfStockProducts = 2
        });

        _orders.GetStatusCountsAsync(null, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new OrderStatusCounts
            {
                TotalOrdersCreated = 4,
                DraftOrders = 1,
                ConfirmedOrders = 1,
                ShippedOrders = 1,
                DeliveredOrders = 1,
                CancelledOrders = 0
            });

        _orders.GetCommercialOrdersAsync(null, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new CommercialOrderAmount { Subtotal = 100m, ConfirmedAtUtc = DateTime.UtcNow },
                new CommercialOrderAmount { Subtotal = 50m, ConfirmedAtUtc = DateTime.UtcNow }
            ]);

        _dashboard.GetPartyCountsAsync(Arg.Any<CancellationToken>()).Returns(new DashboardPartyCounts
        {
            CustomersTotal = 3,
            CustomersActive = 2,
            SuppliersTotal = 4,
            SuppliersActive = 4
        });

        _dashboard.GetPurchaseStatusCountsAsync(Arg.Any<CancellationToken>()).Returns(new DashboardPurchaseStatusCounts
        {
            Total = 5,
            Draft = 1,
            Sent = 1,
            PartiallyReceived = 1,
            Received = 1,
            Cancelled = 1
        });

        _dashboard.GetPurchaseCommercialAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns([new DashboardPurchaseCommercialAmount { Subtotal = 200m, SentAtUtc = DateTime.UtcNow }]);

        _dashboard.GetShipmentStatusCountsAsync(Arg.Any<CancellationToken>()).Returns(new DashboardShipmentStatusCounts
        {
            Preparing = 1,
            Shipped = 1,
            InTransit = 2,
            Delivered = 5,
            Failed = 1
        });

        _dashboard.GetReturnStatusCountsAsync(Arg.Any<CancellationToken>()).Returns(new DashboardReturnStatusCounts
        {
            Draft = 1,
            Authorized = 0,
            Received = 1,
            UnderInspection = 2,
            Completed = 3,
            Dispatched = 0,
            Rejected = 1,
            Cancelled = 1
        });

        _dashboard.GetFinanceOpenMetricsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardFinanceOpenMetrics
            {
                PayablesPendingCount = 2,
                ReceivablesPendingCount = 3,
                OverduePayablesCount = 1,
                OverdueReceivablesCount = 0,
                PayablesPendingAmount = 10m,
                ReceivablesPendingAmount = 20m,
                OverduePayablesAmount = 5m,
                OverdueReceivablesAmount = 0m
            });
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesOutOfStockFromLowStockCount()
    {
        ConfigureUser(Permissions.All);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(null, null, null);

        Assert.Equal(3, result.Products.LowStock);
        Assert.Equal(2, result.Products.OutOfStock);
        Assert.Equal(150m, result.Orders.ConfirmedOrderValue);
        Assert.Equal(2, result.Orders.ConfirmedInPeriod);
    }

    [Fact]
    public async Task ExecuteAsync_OmitsFinance_WhenUserLacksFinanceReportsView()
    {
        ConfigureUser([Permissions.Reports.View, Permissions.Notifications.View]);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(null, null, null);

        Assert.Null(result.Finance);
        await _dashboard.DidNotReceive()
            .GetFinanceOpenMetricsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_IncludesFinance_WhenUserHasFinanceReportsView()
    {
        ConfigureUser(Permissions.All);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(null, null, null);

        Assert.NotNull(result.Finance);
        Assert.Equal(2, result.Finance.PayablesPending);
        Assert.Equal(5m, result.Finance.OverduePayablesAmount);
    }

    [Fact]
    public async Task ExecuteAsync_CountsUnreadNotificationsForCurrentUserOnly()
    {
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        ConfigureUser(Permissions.All, userId);
        _notifications.CountUnreadAsync(userId, Arg.Any<CancellationToken>()).Returns(7);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(null, null, null);

        Assert.NotNull(result.Notifications);
        Assert.Equal(7, result.Notifications.Unread);
        await _notifications.Received(1).CountUnreadAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OmitsNotifications_WhenPermissionMissing()
    {
        ConfigureUser([Permissions.Reports.View, Permissions.Finance.ReportsView]);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(null, null, null);

        Assert.Null(result.Notifications);
        await _notifications.DidNotReceive()
            .CountUnreadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private GetOperationalDashboardSummaryUseCase CreateUseCase() =>
        new(_inventory, _orders, _dashboard, _notifications, _currentUser, _time);

    private void ConfigureUser(IEnumerable<string> permissions, Guid? userId = null)
    {
        _currentUser.UserId.Returns(userId ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        _currentUser.Permissions.Returns(permissions.ToHashSet());
        _currentUser.IsAuthenticated.Returns(true);
    }
}
