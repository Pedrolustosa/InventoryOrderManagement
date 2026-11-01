using InventoryOrderManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public sealed class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("SecurityAuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(log => log.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(log => log.EntityId)
            .HasMaxLength(64);

        builder.Property(log => log.Outcome)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(log => log.CorrelationId)
            .HasMaxLength(128);

        builder.Property(log => log.Details)
            .HasMaxLength(500);

        builder.HasIndex(log => log.OccurredAtUtc);
        builder.HasIndex(log => log.ActorUserId);
        builder.HasIndex(log => log.TargetUserId);
        builder.HasIndex(log => log.Action);
    }
}
