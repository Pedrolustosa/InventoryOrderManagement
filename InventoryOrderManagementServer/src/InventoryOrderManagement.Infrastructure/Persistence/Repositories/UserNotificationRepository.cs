using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly InventoryDbContext _dbContext;

    public UserNotificationRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<UserNotification> Items, int TotalItems)> GetPageAsync(
        UserNotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(_dbContext.UserNotifications.AsNoTracking(), query);
        var totalItems = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
        _dbContext.UserNotifications.AsNoTracking()
            .CountAsync(
                notification => notification.RecipientUserId == recipientUserId && !notification.IsRead,
                cancellationToken);

    public Task<UserNotification?> GetByIdForRecipientAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserNotifications
            .FirstOrDefaultAsync(
                notification => notification.Id == notificationId && notification.RecipientUserId == recipientUserId,
                cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        await _dbContext.UserNotifications.AddRangeAsync(notifications, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Never leave unsaved notifications tracked: a later SaveChanges in the same request would retry them.
            foreach (var notification in notifications)
            {
                _dbContext.Entry(notification).State = EntityState.Detached;
            }

            throw;
        }
    }

    public Task<bool> ExistsUnreadAsync(
        Guid recipientUserId,
        NotificationType type,
        Guid relatedEntityId,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserNotifications.AsNoTracking()
            .AnyAsync(
                notification => notification.RecipientUserId == recipientUserId
                    && notification.Type == type
                    && notification.RelatedEntityId == relatedEntityId
                    && !notification.IsRead,
                cancellationToken);

    public async Task<int> MarkAllUnreadAsReadAsync(
        Guid recipientUserId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.UserNotifications
            .Where(notification => notification.RecipientUserId == recipientUserId && !notification.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkAsRead(readAtUtc);
        }

        return unread.Count;
    }

    public Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken cancellationToken = default) =>
        _dbContext.UserNotifications.AsNoTracking()
            .AnyAsync(notification => notification.EventKey == eventKey, cancellationToken);

    private static IQueryable<UserNotification> ApplyFilters(
        IQueryable<UserNotification> source,
        UserNotificationQuery query)
    {
        var filtered = source.Where(notification => notification.RecipientUserId == query.RecipientUserId);

        if (query.IsRead is { } isRead)
        {
            filtered = filtered.Where(notification => notification.IsRead == isRead);
        }

        if (query.Type is { } type)
        {
            filtered = filtered.Where(notification => notification.Type == type);
        }

        if (query.Severity is { } severity)
        {
            filtered = filtered.Where(notification => notification.Severity == severity);
        }

        return filtered;
    }
}
