using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Purchases;

internal static class PurchaseAudit
{
    public static Task WriteAsync(
        ISecurityAuditWriter writer,
        ICurrentUser currentUser,
        string action,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return writer.WriteAsync(
            new SecurityAuditEntry
            {
                ActorUserId = currentUser.UserId,
                Action = action,
                EntityType = "PurchaseOrder",
                EntityId = entityId.ToString(),
                Outcome = SecurityOutcomes.Success
            },
            cancellationToken);
    }
}
