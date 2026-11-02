using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Notifications.Dtos;

public sealed class NotificationListQuery
{
    public bool? IsRead { get; init; }

    public NotificationType? Type { get; init; }

    public NotificationSeverity? Severity { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed class UnreadNotificationCountResponse
{
    public required int Count { get; init; }
}

public sealed class MarkAllNotificationsAsReadResponse
{
    public required int UpdatedCount { get; init; }
}

public sealed class NotificationResponse
{
    public required Guid Id { get; init; }

    public required NotificationType Type { get; init; }

    public required NotificationSeverity Severity { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? TargetRoute { get; init; }

    public required bool IsRead { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    public static NotificationResponse FromEntity(UserNotification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Severity = notification.Severity,
        Title = notification.Title,
        Message = notification.Message,
        RelatedEntityType = notification.RelatedEntityType,
        RelatedEntityId = notification.RelatedEntityId,
        TargetRoute = notification.TargetRoute,
        IsRead = notification.IsRead,
        CreatedAtUtc = notification.CreatedAtUtc,
        ReadAtUtc = notification.ReadAtUtc
    };
}
