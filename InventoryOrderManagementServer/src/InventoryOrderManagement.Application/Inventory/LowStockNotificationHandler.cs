using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Inventory;

/// <summary>
/// Post-commit handler: notifies every active user holding <see cref="Permissions.Inventory.LowStockAlerts"/>
/// when a product's available stock crosses below its minimum.
/// </summary>
/// <remarks>
/// It runs after the stock change is committed, so it must never fail the operation: every error is logged
/// and swallowed. Without an outbox, a notification that fails (or a process that stops between commit and
/// this handler) is lost and not retried. A user who still has an unread low-stock alert for the product is
/// not notified again.
/// </remarks>
public sealed class LowStockNotificationHandler : IDomainEventHandler<StockBelowMinimumDomainEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LowStockNotificationHandler> _logger;

    public LowStockNotificationHandler(
        IProductRepository productRepository,
        INotificationRecipientResolver recipientResolver,
        IUserNotificationRepository notificationRepository,
        TimeProvider timeProvider,
        ILogger<LowStockNotificationHandler> logger)
    {
        _productRepository = productRepository;
        _recipientResolver = recipientResolver;
        _notificationRepository = notificationRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(
        StockBelowMinimumDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            await NotifyAsync(domainEvent, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Failed to create low-stock notifications for product {ProductId} (available {Available}, minimum {Minimum}).",
                domainEvent.ProductId,
                domainEvent.Available,
                domainEvent.MinimumStock);
        }
    }

    private async Task NotifyAsync(StockBelowMinimumDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var recipients = await _recipientResolver.GetActiveUserIdsByPermissionAsync(
            Permissions.Inventory.LowStockAlerts,
            cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var product = await _productRepository.GetByIdAsync(domainEvent.ProductId, cancellationToken);
        var productLabel = product is null
            ? domainEvent.ProductId.ToString()
            : $"'{product.Name}' ({product.Sku})";

        var outOfStock = domainEvent.Available <= 0;
        var title = outOfStock ? "Product out of stock" : "Low stock detected";
        var message =
            $"Product {productLabel} is below its minimum stock. Available: {domainEvent.Available}. Minimum: {domainEvent.MinimumStock}.";
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var notifications = new List<UserNotification>(recipients.Count);
        foreach (var userId in recipients)
        {
            if (await _notificationRepository.ExistsUnreadAsync(
                    userId,
                    NotificationType.LowStockDetected,
                    domainEvent.ProductId,
                    cancellationToken))
            {
                continue;
            }

            notifications.Add(UserNotification.Create(
                userId,
                NotificationType.LowStockDetected,
                outOfStock ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                title,
                message,
                $"LowStock:{domainEvent.ProductId:N}:{userId:N}:{domainEvent.OccurredOnUtc.Ticks}",
                "Product",
                domainEvent.ProductId,
                $"/inventory?productId={domainEvent.ProductId}",
                now));
        }

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
    }
}
