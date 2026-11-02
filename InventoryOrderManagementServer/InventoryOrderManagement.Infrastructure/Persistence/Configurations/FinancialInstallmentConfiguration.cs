using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class FinancialInstallmentConfiguration : IEntityTypeConfiguration<FinancialInstallment>
{
    public void Configure(EntityTypeBuilder<FinancialInstallment> builder)
    {
        builder.ToTable("FinancialInstallments");
        builder.HasKey(installment => installment.Id);

        builder.Property(installment => installment.FinancialTitleId).IsRequired();
        builder.HasIndex(installment => new { installment.FinancialTitleId, installment.InstallmentNumber }).IsUnique();
        builder.Property(installment => installment.DueDate).IsRequired();
        builder.HasIndex(installment => installment.DueDate);
        builder.Property(installment => installment.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(installment => installment.SettledPrincipalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(installment => installment.OutstandingBalance).IsRequired().HasPrecision(18, 2);
        builder.Property(installment => installment.CreatedAtUtc).IsRequired();
        builder.Property(installment => installment.Version).IsRequired().IsConcurrencyToken();
    }
}
