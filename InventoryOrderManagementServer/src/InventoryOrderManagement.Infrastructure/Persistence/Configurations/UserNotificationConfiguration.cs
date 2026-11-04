using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.RecipientUserId)
            .IsRequired();

        builder.HasIndex(entity => entity.RecipientUserId);
        builder.HasIndex(entity => entity.IsRead);
        builder.HasIndex(entity => entity.CreatedAtUtc);
        builder.HasIndex(entity => entity.Type);
        builder.HasIndex(entity => entity.EventKey)
            .IsUnique();

        builder.Property(entity => entity.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(entity => entity.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(entity => entity.Title)
            .IsRequired()
            .HasMaxLength(UserNotification.TitleMaxLength);

        builder.Property(entity => entity.Message)
            .IsRequired()
            .HasMaxLength(UserNotification.MessageMaxLength);

        builder.Property(entity => entity.RelatedEntityType)
            .HasMaxLength(UserNotification.RelatedEntityTypeMaxLength);

        builder.Property(entity => entity.TargetRoute)
            .HasMaxLength(UserNotification.TargetRouteMaxLength);

        builder.Property(entity => entity.EventKey)
            .IsRequired()
            .HasMaxLength(UserNotification.EventKeyMaxLength);

        builder.Property(entity => entity.IsRead)
            .IsRequired();

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
