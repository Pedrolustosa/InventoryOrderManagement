namespace InventoryOrderManagement.Application.Abstractions.Notifications;

public interface INotificationDispatcher
{
    Task NotifyOrderConfirmedAsync(
        Guid orderId,
        string orderNumber,
        string customerName,
        DateTime confirmedAtUtc,
        CancellationToken cancellationToken = default);

    Task NotifyShipmentFailedAsync(
        Guid shipmentId,
        string orderNumber,
        Guid shipmentStatusHistoryId,
        string failureReason,
        DateTime failedAtUtc,
        CancellationToken cancellationToken = default);

    Task NotifyReturnAwaitingInspectionAsync(
        Guid returnId,
        string returnNumber,
        string customerName,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default);
}
