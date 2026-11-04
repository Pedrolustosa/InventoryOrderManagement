using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");
        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.CashAccountId).IsRequired();
        builder.HasIndex(movement => movement.CashAccountId);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(movement => movement.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.Settlement)
            .WithMany()
            .HasForeignKey(movement => movement.FinancialSettlementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(movement => movement.FinancialSettlementId)
            .IsUnique()
            .HasFilter("\"FinancialSettlementId\" IS NOT NULL");

        builder.Property(movement => movement.Direction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(movement => movement.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(movement => movement.Description)
            .IsRequired()
            .HasMaxLength(CashMovement.DescriptionMaxLength);
        builder.Property(movement => movement.OccurredAtUtc).IsRequired();
        builder.HasIndex(movement => movement.OccurredAtUtc);
        builder.Property(movement => movement.CreatedAtUtc).IsRequired();
    }
}
