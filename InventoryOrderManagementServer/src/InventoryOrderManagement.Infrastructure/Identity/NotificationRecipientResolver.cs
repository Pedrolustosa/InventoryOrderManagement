using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class NotificationRecipientResolver : INotificationRecipientResolver
{
    private readonly InventoryDbContext _dbContext;

    public NotificationRecipientResolver(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveUserIdsByPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return [];
        }

        var userIds = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join roleClaim in _dbContext.RoleClaims.AsNoTracking() on userRole.RoleId equals roleClaim.RoleId
            where user.IsActive
                  && roleClaim.ClaimType == Permissions.ClaimType
                  && roleClaim.ClaimValue == permission
            select user.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return userIds;
    }
}
