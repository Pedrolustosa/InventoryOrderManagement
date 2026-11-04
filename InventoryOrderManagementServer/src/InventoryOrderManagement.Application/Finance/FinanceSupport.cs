using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Finance;

internal static class FinanceAudit
{
    public static Task WriteAsync(
        ISecurityAuditWriter writer,
        ICurrentUser currentUser,
        string action,
        string entityType,
        Guid entityId,
        string? details,
        CancellationToken cancellationToken)
    {
        return writer.WriteAsync(
            new SecurityAuditEntry
            {
                ActorUserId = currentUser.UserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId.ToString(),
                Outcome = SecurityOutcomes.Success,
                Details = details
            },
            cancellationToken);
    }
}

internal static class BusinessClock
{
    public static DateTime UtcNow(TimeProvider timeProvider) => timeProvider.GetUtcNow().UtcDateTime;

    public static DateOnly Today(TimeProvider timeProvider) => DateOnly.FromDateTime(UtcNow(timeProvider));
}
