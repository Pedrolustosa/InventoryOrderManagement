namespace InventoryOrderManagement.Application.Abstractions.Notifications;

public interface INotificationRecipientResolver
{
    Task<IReadOnlyCollection<Guid>> GetActiveUserIdsByPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);
}
