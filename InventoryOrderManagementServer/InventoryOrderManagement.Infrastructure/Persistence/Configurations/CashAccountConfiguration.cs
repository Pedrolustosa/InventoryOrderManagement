using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.ToTable("CashAccounts");
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Name)
            .IsRequired()
            .HasMaxLength(CashAccount.NameMaxLength);
        builder.HasIndex(account => account.Name);

        builder.Property(account => account.AccountType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(account => account.OpeningBalance).IsRequired().HasPrecision(18, 2);
        builder.Property(account => account.OpeningBalanceDate).IsRequired();
        builder.Property(account => account.IsActive).IsRequired();
        builder.Property(account => account.CreatedAtUtc).IsRequired();
    }
}
