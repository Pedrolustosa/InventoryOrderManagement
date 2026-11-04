using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class PurchaseReceiptItemConfiguration : IEntityTypeConfiguration<PurchaseReceiptItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptItem> builder)
    {
        builder.ToTable("PurchaseReceiptItems", table =>
        {
            table.HasCheckConstraint("CK_PurchaseReceiptItems_QuantityReceived", "\"QuantityReceived\" > 0");
            table.HasCheckConstraint("CK_PurchaseReceiptItems_UnitCostSnapshot", "\"UnitCostSnapshot\" >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.PurchaseReceiptId)
            .IsRequired();

        builder.Property(item => item.PurchaseOrderItemId)
            .IsRequired();

        builder.HasOne<PurchaseOrderItem>()
            .WithMany()
            .HasForeignKey(item => item.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(item => item.ProductId)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(item => item.ProductSku)
            .IsRequired()
            .HasMaxLength(Product.SkuMaxLength);

        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(item => item.QuantityReceived)
            .IsRequired();

        builder.Property(item => item.UnitCostSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(item => new { item.PurchaseReceiptId, item.PurchaseOrderItemId })
            .IsUnique();

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.PurchaseOrderItemId);
    }
}
