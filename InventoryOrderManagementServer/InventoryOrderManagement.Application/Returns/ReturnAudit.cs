using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Returns;

internal static class ReturnAudit
{
    public static Task WriteAsync(
        ISecurityAuditWriter writer,
        ICurrentUser currentUser,
        string action,
        Guid entityId,
        CancellationToken cancellationToken,
        string? details = null)
    {
        return writer.WriteAsync(
            new SecurityAuditEntry
            {
                ActorUserId = currentUser.UserId,
                Action = action,
                EntityType = SecurityEntities.ReturnRequest,
                EntityId = entityId.ToString(),
                Outcome = SecurityOutcomes.Success,
                Details = details
            },
            cancellationToken);
    }
}
