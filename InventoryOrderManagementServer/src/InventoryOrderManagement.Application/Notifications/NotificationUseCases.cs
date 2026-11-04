using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Notifications;

public sealed class GetUserNotificationsUseCase
{
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;

    public GetUserNotificationsUseCase(
        IUserNotificationRepository notificationRepository,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationResponse>> ExecuteAsync(
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = RequireUserId();
        var (page, pageSize) = PageRequest.Normalize(query.Page, query.PageSize);

        var (items, totalItems) = await _notificationRepository.GetPageAsync(
            new UserNotificationQuery
            {
                RecipientUserId = userId,
                IsRead = query.IsRead,
                Type = query.Type,
                Severity = query.Severity,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<NotificationResponse>.Create(
            items.Select(NotificationResponse.FromEntity).ToList(),
            page,
            pageSize,
            totalItems);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new NotificationUserRequiredException();
}

public sealed class GetUnreadNotificationCountUseCase
{
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;

    public GetUnreadNotificationCountUseCase(
        IUserNotificationRepository notificationRepository,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<UnreadNotificationCountResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new NotificationUserRequiredException();
        var count = await _notificationRepository.CountUnreadAsync(userId, cancellationToken);
        return new UnreadNotificationCountResponse { Count = count };
    }
}

public sealed class MarkNotificationAsReadUseCase
{
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadUseCase(
        IUserNotificationRepository notificationRepository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationResponse> ExecuteAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new NotificationUserRequiredException();
        NotificationResponse? response = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var notification = await _notificationRepository.GetByIdForRecipientAsync(notificationId, userId, ct)
                ?? throw new NotificationNotFoundException(notificationId);

            notification.MarkAsRead(DateTime.UtcNow);
            response = NotificationResponse.FromEntity(notification);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Notification was not updated.");
    }
}

public sealed class MarkAllNotificationsAsReadUseCase
{
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsAsReadUseCase(
        IUserNotificationRepository notificationRepository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkAllNotificationsAsReadResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new NotificationUserRequiredException();
        var updated = 0;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            updated = await _notificationRepository.MarkAllUnreadAsReadAsync(userId, DateTime.UtcNow, ct);
        }, cancellationToken);

        return new MarkAllNotificationsAsReadResponse { UpdatedCount = updated };
    }
}
