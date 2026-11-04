using InventoryOrderManagement.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Notifications;

public static class NotificationDispatch
{
    public static async Task TryNotifyLowStockAsync(
        INotificationDispatcher dispatcher,
        ILogger logger,
        IEnumerable<LowStockChange> changes,
        CancellationToken cancellationToken = default)
    {
        foreach (var change in changes)
        {
            try
            {
                await dispatcher.NotifyLowStockAsync(
                    change.ProductId,
                    change.ProductName,
                    change.ProductSku,
                    change.PreviousQuantity,
                    change.NewQuantity,
                    change.MinimumStockLevel,
                    change.StockMovementId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to dispatch low-stock notification for product {ProductId} after stock movement {StockMovementId}.",
                    change.ProductId,
                    change.StockMovementId);
            }
        }
    }

    public static async Task TryAsync(
        Func<CancellationToken, Task> action,
        ILogger logger,
        string failureMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await action(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", failureMessage);
        }
    }
}
