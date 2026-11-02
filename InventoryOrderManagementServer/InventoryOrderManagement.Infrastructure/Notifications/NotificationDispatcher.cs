using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Infrastructure.Notifications;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationRecipientResolver recipientResolver,
        IUserNotificationRepository notificationRepository,
        ILogger<NotificationDispatcher> logger)
    {
        _recipientResolver = recipientResolver;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public Task NotifyLowStockAsync(
        Guid productId,
        string productName,
        string productSku,
        int previousQuantity,
        int newQuantity,
        int minimumStockLevel,
        Guid stockMovementId,
        CancellationToken cancellationToken = default)
    {
        if (!LowStockNotificationRules.ShouldNotify(previousQuantity, newQuantity, minimumStockLevel))
        {
            return Task.CompletedTask;
        }

        var critical = LowStockNotificationRules.IsCritical(newQuantity);
        var severity = critical ? NotificationSeverity.Critical : NotificationSeverity.Warning;
        var title = critical ? "Product out of stock" : "Low stock detected";
        var message = critical
            ? $"Product '{productName}' ({productSku}) is out of stock."
            : $"Product '{productName}' ({productSku}) is at or below the minimum stock level ({minimumStockLevel}). Current quantity: {newQuantity}.";

        return DispatchAsync(
            Permissions.Inventory.View,
            userId => UserNotification.Create(
                userId,
                NotificationType.LowStockDetected,
                severity,
                title,
                message,
                $"LowStockDetected:{stockMovementId}:{userId}",
                "Product",
                productId,
                "/inventory",
                DateTime.UtcNow),
            cancellationToken);
    }

    public Task NotifyOrderConfirmedAsync(
        Guid orderId,
        string orderNumber,
        string customerName,
        DateTime confirmedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var confirmedLocal = confirmedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(confirmedAtUtc, DateTimeKind.Utc)
            : confirmedAtUtc.ToUniversalTime();

        return DispatchAsync(
            Permissions.Shipments.View,
            userId => UserNotification.Create(
                userId,
                NotificationType.OrderConfirmed,
                NotificationSeverity.Info,
                "Sales order confirmed",
                $"Order {orderNumber} for customer '{customerName}' was confirmed on {confirmedLocal:u} and is ready for shipping.",
                $"OrderConfirmed:{orderId}:{userId}",
                "Order",
                orderId,
                $"/orders/{orderId}",
                DateTime.UtcNow),
            cancellationToken);
    }

    public Task NotifyShipmentFailedAsync(
        Guid shipmentId,
        string orderNumber,
        Guid shipmentStatusHistoryId,
        string failureReason,
        DateTime failedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var failedLocal = failedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(failedAtUtc, DateTimeKind.Utc)
            : failedAtUtc.ToUniversalTime();

        return DispatchAsync(
            Permissions.Shipments.View,
            userId => UserNotification.Create(
                userId,
                NotificationType.ShipmentFailed,
                NotificationSeverity.Critical,
                "Shipment failed",
                $"Shipment for order {orderNumber} failed on {failedLocal:u}. Reason: {failureReason}",
                $"ShipmentFailed:{shipmentStatusHistoryId}:{userId}",
                "Shipment",
                shipmentId,
                $"/shipping/{shipmentId}",
                DateTime.UtcNow),
            cancellationToken);
    }

    public Task NotifyReturnAwaitingInspectionAsync(
        Guid returnId,
        string returnNumber,
        string customerName,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var receivedLocal = receivedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc)
            : receivedAtUtc.ToUniversalTime();

        return DispatchAsync(
            Permissions.Returns.View,
            userId => UserNotification.Create(
                userId,
                NotificationType.ReturnAwaitingInspection,
                NotificationSeverity.Warning,
                "Return awaiting inspection",
                $"Customer return {returnNumber} for '{customerName}' was received on {receivedLocal:u} and awaits inspection.",
                $"ReturnAwaitingInspection:{returnId}:{userId}",
                "ReturnRequest",
                returnId,
                $"/returns/{returnId}",
                DateTime.UtcNow),
            cancellationToken);
    }

    private async Task DispatchAsync(
        string permission,
        Func<Guid, UserNotification> factory,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await _recipientResolver.GetActiveUserIdsByPermissionAsync(permission, cancellationToken);
            if (recipients.Count == 0)
            {
                return;
            }

            var notifications = new List<UserNotification>(recipients.Count);
            foreach (var userId in recipients)
            {
                var notification = factory(userId);
                if (await _notificationRepository.ExistsByEventKeyAsync(notification.EventKey, cancellationToken))
                {
                    continue;
                }

                notifications.Add(notification);
            }

            if (notifications.Count == 0)
            {
                return;
            }

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Notification persistence conflict for permission {Permission}.", permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch notifications for permission {Permission}.", permission);
        }
    }
}
