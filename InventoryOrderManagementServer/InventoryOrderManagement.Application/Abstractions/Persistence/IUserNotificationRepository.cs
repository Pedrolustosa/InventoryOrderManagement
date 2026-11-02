using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IUserNotificationRepository
{
    Task<(IReadOnlyList<UserNotification> Items, int TotalItems)> GetPageAsync(
        UserNotificationQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<UserNotification?> GetByIdForRecipientAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<UserNotification> notifications, CancellationToken cancellationToken = default);

    Task<int> MarkAllUnreadAsReadAsync(
        Guid recipientUserId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken cancellationToken = default);
}

public sealed class UserNotificationQuery
{
    public required Guid RecipientUserId { get; init; }

    public bool? IsRead { get; init; }

    public NotificationType? Type { get; init; }

    public NotificationSeverity? Severity { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
