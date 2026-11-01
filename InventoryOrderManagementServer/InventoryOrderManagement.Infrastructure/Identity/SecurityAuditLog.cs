namespace InventoryOrderManagement.Infrastructure.Identity;

public class SecurityAuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? TargetUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? Details { get; set; }
}
