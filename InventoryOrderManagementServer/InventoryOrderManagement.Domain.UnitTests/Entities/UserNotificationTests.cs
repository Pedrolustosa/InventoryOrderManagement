using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class UserNotificationTests
{
    [Fact]
    public void Create_StartsUnread_WithImmutableContent()
    {
        var recipientId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

        var notification = UserNotification.Create(
            recipientId,
            NotificationType.LowStockDetected,
            NotificationSeverity.Warning,
            "Low stock detected",
            "Product is low.",
            $"LowStockDetected:{Guid.NewGuid()}:{recipientId}",
            "Product",
            relatedId,
            "/inventory",
            createdAt);

        Assert.Equal(recipientId, notification.RecipientUserId);
        Assert.Equal(NotificationType.LowStockDetected, notification.Type);
        Assert.Equal(NotificationSeverity.Warning, notification.Severity);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
        Assert.Equal(createdAt, notification.CreatedAtUtc);
        Assert.Equal(relatedId, notification.RelatedEntityId);
        Assert.Equal("/inventory", notification.TargetRoute);
    }

    [Fact]
    public void MarkAsRead_SetsFlagOnce()
    {
        var notification = UserNotification.Create(
            Guid.NewGuid(),
            NotificationType.OrderConfirmed,
            NotificationSeverity.Info,
            "Sales order confirmed",
            "Order ready.",
            $"OrderConfirmed:{Guid.NewGuid()}:{Guid.NewGuid()}",
            "Order",
            Guid.NewGuid(),
            "/orders/1",
            DateTime.UtcNow);

        var readAt = new DateTime(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);
        notification.MarkAsRead(readAt);

        Assert.True(notification.IsRead);
        Assert.Equal(readAt, notification.ReadAtUtc);

        notification.MarkAsRead(DateTime.UtcNow.AddHours(1));
        Assert.Equal(readAt, notification.ReadAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RequiresTitle(string title)
    {
        Assert.Throws<DomainException>(() => UserNotification.Create(
            Guid.NewGuid(),
            NotificationType.ShipmentFailed,
            NotificationSeverity.Critical,
            title,
            "message",
            "key",
            null,
            null,
            null,
            DateTime.UtcNow));
    }

    [Fact]
    public void Create_RejectsEmptyRecipient()
    {
        Assert.Throws<DomainException>(() => UserNotification.Create(
            Guid.Empty,
            NotificationType.ReturnAwaitingInspection,
            NotificationSeverity.Warning,
            "title",
            "message",
            "key",
            null,
            null,
            null,
            DateTime.UtcNow));
    }
}
