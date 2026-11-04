namespace InventoryOrderManagement.Application.Exceptions;

public sealed class NotificationNotFoundException : Exception
{
    public NotificationNotFoundException(Guid notificationId)
        : base($"Notification '{notificationId}' was not found.")
    {
        NotificationId = notificationId;
    }

    public Guid NotificationId { get; }
}

public sealed class NotificationUserRequiredException : Exception
{
    public NotificationUserRequiredException()
        : base("Authentication is required to access notifications.")
    {
    }
}
