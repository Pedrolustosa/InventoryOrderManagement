using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Notifications;

public static class NotificationDispatch
{
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
