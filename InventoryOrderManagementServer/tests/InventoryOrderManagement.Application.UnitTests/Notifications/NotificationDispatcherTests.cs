using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class NotificationDispatcherTests
{
    private readonly INotificationRecipientResolver _recipients = Substitute.For<INotificationRecipientResolver>();
    private readonly IUserNotificationRepository _repository = Substitute.For<IUserNotificationRepository>();
    private readonly ILogger<NotificationDispatcher> _logger = Substitute.For<ILogger<NotificationDispatcher>>();
    private readonly NotificationDispatcher _dispatcher;

    public NotificationDispatcherTests()
    {
        _dispatcher = new NotificationDispatcher(_recipients, _repository, _logger);
        _repository.ExistsByEventKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    [Fact]
    public async Task NotifyLowStock_CrossingMinimum_CreatesWarningForInventoryViewUsers()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.View, Arg.Any<CancellationToken>())
            .Returns([userId]);

        await _dispatcher.NotifyLowStockAsync(
            Guid.NewGuid(),
            "Widget",
            "SKU-1",
            previousQuantity: 15,
            newQuantity: 4,
            minimumStockLevel: 5,
            Guid.NewGuid());

        await _repository.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<UserNotification>>(items =>
                items.Count == 1
                && items.Single().RecipientUserId == userId
                && items.Single().Type == NotificationType.LowStockDetected
                && items.Single().Severity == NotificationSeverity.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyLowStock_DepletingToZero_CreatesCritical()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.View, Arg.Any<CancellationToken>())
            .Returns([userId]);

        await _dispatcher.NotifyLowStockAsync(
            Guid.NewGuid(),
            "Widget",
            "SKU-1",
            previousQuantity: 3,
            newQuantity: 0,
            minimumStockLevel: 5,
            Guid.NewGuid());

        await _repository.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<UserNotification>>(items =>
                items.Single().Severity == NotificationSeverity.Critical
                && items.Single().Title == "Product out of stock"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyLowStock_WhileAlreadyBelow_DoesNotPersist()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.View, Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);

        await _dispatcher.NotifyLowStockAsync(
            Guid.NewGuid(),
            "Widget",
            "SKU-1",
            previousQuantity: 4,
            newQuantity: 3,
            minimumStockLevel: 5,
            Guid.NewGuid());

        await _repository.DidNotReceive().AddRangeAsync(
            Arg.Any<IReadOnlyCollection<UserNotification>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyOrderConfirmed_TargetsShipmentsView()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Shipments.View, Arg.Any<CancellationToken>())
            .Returns([userId]);

        await _dispatcher.NotifyOrderConfirmedAsync(Guid.NewGuid(), "ORD-1", "Jane", DateTime.UtcNow);

        await _repository.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<UserNotification>>(items =>
                items.Single().Type == NotificationType.OrderConfirmed
                && items.Single().Severity == NotificationSeverity.Info
                && items.Single().RecipientUserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyShipmentFailed_IsCritical()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Shipments.View, Arg.Any<CancellationToken>())
            .Returns([userId]);

        await _dispatcher.NotifyShipmentFailedAsync(
            Guid.NewGuid(),
            "ORD-1",
            Guid.NewGuid(),
            "Customer absent",
            DateTime.UtcNow);

        await _repository.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<UserNotification>>(items =>
                items.Single().Type == NotificationType.ShipmentFailed
                && items.Single().Severity == NotificationSeverity.Critical),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyReturnAwaitingInspection_TargetsReturnsView()
    {
        var userId = Guid.NewGuid();
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Returns.View, Arg.Any<CancellationToken>())
            .Returns([userId]);

        await _dispatcher.NotifyReturnAwaitingInspectionAsync(
            Guid.NewGuid(),
            "RET-1",
            "Jane",
            DateTime.UtcNow);

        await _repository.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<UserNotification>>(items =>
                items.Single().Type == NotificationType.ReturnAwaitingInspection
                && items.Single().Severity == NotificationSeverity.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_WhenRepositoryThrows_LogsAndDoesNotRethrow()
    {
        _recipients.GetActiveUserIdsByPermissionAsync(Permissions.Inventory.View, Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);
        _repository
            .When(repo => repo.AddRangeAsync(Arg.Any<IReadOnlyCollection<UserNotification>>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("db down"));

        var exception = await Record.ExceptionAsync(() => _dispatcher.NotifyLowStockAsync(
            Guid.NewGuid(),
            "Widget",
            "SKU-1",
            15,
            4,
            5,
            Guid.NewGuid()));

        Assert.Null(exception);
        Assert.Contains(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }
}
