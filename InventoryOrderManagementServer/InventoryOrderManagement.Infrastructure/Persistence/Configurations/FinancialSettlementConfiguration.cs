using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class FinancialSettlementConfiguration : IEntityTypeConfiguration<FinancialSettlement>
{
    public void Configure(EntityTypeBuilder<FinancialSettlement> builder)
    {
        builder.ToTable("FinancialSettlements");
        builder.HasKey(settlement => settlement.Id);

        builder.Property(settlement => settlement.FinancialTitleId).IsRequired();
        builder.HasIndex(settlement => settlement.FinancialTitleId);

        builder.Property(settlement => settlement.FinancialInstallmentId).IsRequired();
        builder.HasIndex(settlement => settlement.FinancialInstallmentId);

        builder.HasOne<FinancialInstallment>()
            .WithMany()
            .HasForeignKey(settlement => settlement.FinancialInstallmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(settlement => settlement.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(settlement => settlement.PaymentMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(settlement => settlement.SettledPrincipalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(settlement => settlement.DiscountAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(settlement => settlement.InterestAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(settlement => settlement.FineAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(settlement => settlement.CashAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(settlement => settlement.OccurredAtUtc).IsRequired();
        builder.HasIndex(settlement => settlement.OccurredAtUtc);
        builder.Property(settlement => settlement.Reference).HasMaxLength(FinancialSettlement.ReferenceMaxLength);
        builder.Property(settlement => settlement.Notes).HasMaxLength(FinancialSettlement.NotesMaxLength);
        builder.Property(settlement => settlement.IdempotencyKey).HasMaxLength(FinancialSettlement.IdempotencyKeyMaxLength);
        builder.HasIndex(settlement => settlement.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.Property(settlement => settlement.CreatedAtUtc).IsRequired();
    }
}
