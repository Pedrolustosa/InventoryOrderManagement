using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", table =>
        {
            table.HasCheckConstraint("CK_PurchaseOrderItems_OrderedQuantity", "\"OrderedQuantity\" > 0");
            table.HasCheckConstraint("CK_PurchaseOrderItems_ReceivedQuantity", "\"ReceivedQuantity\" >= 0");
            table.HasCheckConstraint("CK_PurchaseOrderItems_UnitCost", "\"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_PurchaseOrderItems_LineTotal", "\"LineTotal\" >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.PurchaseOrderId)
            .IsRequired();

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

        builder.Property(item => item.OrderedQuantity)
            .IsRequired();

        builder.Property(item => item.ReceivedQuantity)
            .IsRequired();

        builder.Property(item => item.UnitCost)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(item => new { item.PurchaseOrderId, item.ProductId })
            .IsUnique();

        builder.HasIndex(item => item.ProductId);
    }
}
