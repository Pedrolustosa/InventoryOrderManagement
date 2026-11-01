using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances", table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryBalances_QuantityOnHand",
                "\"QuantityOnHand\" >= 0");
            table.HasCheckConstraint(
                "CK_InventoryBalances_MinimumStockLevel",
                "\"MinimumStockLevel\" >= 0");
        });

        builder.HasKey(balance => balance.ProductId);

        builder.Property(balance => balance.QuantityOnHand)
            .IsRequired();

        builder.Property(balance => balance.MinimumStockLevel)
            .IsRequired();

        builder.Property(balance => balance.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasOne<Product>()
            .WithOne()
            .HasForeignKey<InventoryBalance>(balance => balance.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
