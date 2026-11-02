using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Notifications.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class NotificationsController : ControllerBase
{
    private readonly GetUserNotificationsUseCase _getUserNotificationsUseCase;
    private readonly GetUnreadNotificationCountUseCase _getUnreadNotificationCountUseCase;
    private readonly MarkNotificationAsReadUseCase _markNotificationAsReadUseCase;
    private readonly MarkAllNotificationsAsReadUseCase _markAllNotificationsAsReadUseCase;

    public NotificationsController(
        GetUserNotificationsUseCase getUserNotificationsUseCase,
        GetUnreadNotificationCountUseCase getUnreadNotificationCountUseCase,
        MarkNotificationAsReadUseCase markNotificationAsReadUseCase,
        MarkAllNotificationsAsReadUseCase markAllNotificationsAsReadUseCase)
    {
        _getUserNotificationsUseCase = getUserNotificationsUseCase;
        _getUnreadNotificationCountUseCase = getUnreadNotificationCountUseCase;
        _markNotificationAsReadUseCase = markNotificationAsReadUseCase;
        _markAllNotificationsAsReadUseCase = markAllNotificationsAsReadUseCase;
    }

    [Authorize(Policy = Permissions.Notifications.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<PagedResult<NotificationResponse>> GetAll(
        [FromQuery] bool? isRead,
        [FromQuery] NotificationType? type,
        [FromQuery] NotificationSeverity? severity,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return _getUserNotificationsUseCase.ExecuteAsync(
            new NotificationListQuery
            {
                IsRead = isRead,
                Type = type,
                Severity = severity,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);
    }

    [Authorize(Policy = Permissions.Notifications.View)]
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountResponse), StatusCodes.Status200OK)]
    public Task<UnreadNotificationCountResponse> GetUnreadCount(CancellationToken cancellationToken) =>
        _getUnreadNotificationCountUseCase.ExecuteAsync(cancellationToken);

    [Authorize(Policy = Permissions.Notifications.MarkRead)]
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<NotificationResponse> MarkAsRead(Guid id, CancellationToken cancellationToken) =>
        _markNotificationAsReadUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Notifications.MarkRead)]
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(MarkAllNotificationsAsReadResponse), StatusCodes.Status200OK)]
    public Task<MarkAllNotificationsAsReadResponse> MarkAllAsRead(CancellationToken cancellationToken) =>
        _markAllNotificationsAsReadUseCase.ExecuteAsync(cancellationToken);
}
