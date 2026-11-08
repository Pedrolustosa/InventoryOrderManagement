using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InventoryOrderManagement.Application.UnitTests.Inventory;

public class LowStockNotificationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 9, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly INotificationRecipientResolver _recipients = Substitute.For<INotificationRecipientResolver>();
    private readonly IUserNotificationRepository _notifications = Substitute.For<IUserNotificationRepository>();
    private readonly ILogger<LowStockNotificationHandler> _logger = Substitute.For<ILogger<LowStockNotificationHandler>>();
    private readonly Product _product = Product.Create("SKU-LOW", "Low Widget", null, 10m, Guid.NewGuid());
    private readonly List<UserNotification> _created = [];

    public LowStockNotificationHandlerTests()
    {
        _products.GetByIdAsync(_product.Id, Arg.Any<CancellationToken>()).Returns(_product);
        _notifications.AddRangeAsync(
                Arg.Do<IReadOnlyCollection<UserNotification>>(items => _created.AddRange(items)),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_NotifiesOnlyUsersWithTheLowStockAlertPermission()
    {
        var operatorId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([operatorId, managerId]);

        await Handler().HandleAsync(Event(available: 3, minimum: 5));

        await _recipients.DidNotReceive().GetActiveUserIdsByPermissionAsync(
            Permissions.Inventory.View,
            Arg.Any<CancellationToken>());
        Assert.Equal([operatorId, managerId], _created.Select(item => item.RecipientUserId));
        var notification = _created[0];
        Assert.Equal(NotificationType.LowStockDetected, notification.Type);
        Assert.Equal(NotificationSeverity.Warning, notification.Severity);
        Assert.Equal("Product", notification.RelatedEntityType);
        Assert.Equal(_product.Id, notification.RelatedEntityId);
        Assert.Equal($"/inventory?productId={_product.Id}", notification.TargetRoute);
        Assert.Contains("Low Widget", notification.Message);
        Assert.Contains("SKU-LOW", notification.Message);
        Assert.Contains("Available: 3", notification.Message);
        Assert.Contains("Minimum: 5", notification.Message);
    }

    [Fact]
    public async Task HandleAsync_WithNothingAvailable_IsCritical()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);

        await Handler().HandleAsync(Event(available: 0, minimum: 5));

        var notification = Assert.Single(_created);
        Assert.Equal(NotificationSeverity.Critical, notification.Severity);
        Assert.Equal("Product out of stock", notification.Title);
    }

    [Fact]
    public async Task HandleAsync_SkipsUsersWithAnUnreadLowStockAlertForTheProduct()
    {
        var alreadyAlerted = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([alreadyAlerted, fresh]);
        _notifications.ExistsUnreadAsync(alreadyAlerted, NotificationType.LowStockDetected, _product.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        await Handler().HandleAsync(Event(available: 2, minimum: 5));

        Assert.Equal(fresh, Assert.Single(_created).RecipientUserId);
    }

    [Fact]
    public async Task HandleAsync_AfterThePreviousAlertWasRead_NotifiesAgain()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([userId]);
        _notifications.ExistsUnreadAsync(userId, NotificationType.LowStockDetected, _product.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await Handler().HandleAsync(Event(available: 2, minimum: 5));

        Assert.Single(_created);
    }

    [Fact]
    public async Task HandleAsync_WithoutRecipients_CreatesNothing()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([]);

        await Handler().HandleAsync(Event(available: 2, minimum: 5));

        await _notifications.DidNotReceive().AddRangeAsync(
            Arg.Any<IReadOnlyCollection<UserNotification>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenPersistenceFails_LogsAndDoesNotThrow()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.LowStockAlerts, Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);
        _notifications.AddRangeAsync(Arg.Any<IReadOnlyCollection<UserNotification>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database is down"));

        var exception = await Record.ExceptionAsync(() => Handler().HandleAsync(Event(available: 2, minimum: 5)));

        Assert.Null(exception);
        Assert.Contains(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
    }

    [Fact]
    public async Task HandleAsync_WhenRecipientLookupFails_LogsAndDoesNotThrow()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("identity store unavailable"));

        var exception = await Record.ExceptionAsync(() => Handler().HandleAsync(Event(available: 2, minimum: 5)));

        Assert.Null(exception);
        Assert.Empty(_created);
    }

    private LowStockNotificationHandler Handler() =>
        new(_products, _recipients, _notifications, new FixedTimeProvider(Now), _logger);

    private StockBelowMinimumDomainEvent Event(int available, int minimum) =>
        new(_product.Id, available, minimum, Now.UtcDateTime);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
