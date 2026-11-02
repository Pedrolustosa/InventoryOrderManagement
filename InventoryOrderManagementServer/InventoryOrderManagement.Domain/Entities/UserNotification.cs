using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// In-app notification addressed to a single user. Content is immutable after create.
/// </summary>
public class UserNotification
{
    public const int TitleMaxLength = 200;
    public const int MessageMaxLength = 1000;
    public const int RelatedEntityTypeMaxLength = 100;
    public const int TargetRouteMaxLength = 300;
    public const int EventKeyMaxLength = 200;

    private UserNotification()
    {
    }

    public Guid Id { get; private set; }

    public Guid RecipientUserId { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationSeverity Severity { get; private set; }

    public string Title { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public string? RelatedEntityType { get; private set; }

    public Guid? RelatedEntityId { get; private set; }

    public string? TargetRoute { get; private set; }

    public string EventKey { get; private set; } = null!;

    public bool IsRead { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public static UserNotification Create(
        Guid recipientUserId,
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string eventKey,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? targetRoute,
        DateTime createdAtUtc)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new DomainException("Notification recipient is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Notification type is invalid.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new DomainException("Notification severity is invalid.");
        }

        var normalizedTitle = NormalizeRequired(title, nameof(title), TitleMaxLength);
        var normalizedMessage = NormalizeRequired(message, nameof(message), MessageMaxLength);
        var normalizedEventKey = NormalizeRequired(eventKey, nameof(eventKey), EventKeyMaxLength);

        return new UserNotification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Severity = severity,
            Title = normalizedTitle,
            Message = normalizedMessage,
            RelatedEntityType = NormalizeOptional(relatedEntityType, RelatedEntityTypeMaxLength),
            RelatedEntityId = relatedEntityId == Guid.Empty ? null : relatedEntityId,
            TargetRoute = NormalizeOptional(targetRoute, TargetRouteMaxLength),
            EventKey = normalizedEventKey,
            IsRead = false,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc),
            ReadAtUtc = null
        };
    }

    public void MarkAsRead(DateTime utcNow)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
