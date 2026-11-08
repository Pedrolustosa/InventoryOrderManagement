using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class ReturnReplacementItemConfiguration : IEntityTypeConfiguration<ReturnReplacementItem>
{
    public void Configure(EntityTypeBuilder<ReturnReplacementItem> builder)
    {
        builder.ToTable("ReturnReplacementItems", table =>
        {
            table.HasCheckConstraint("CK_ReturnReplacementItems_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_ReturnReplacementItems_UnitPriceSnapshot", "\"UnitPriceSnapshot\" >= 0");
            table.HasCheckConstraint("CK_ReturnReplacementItems_LineAmount", "\"LineAmount\" >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductSku)
            .IsRequired()
            .HasMaxLength(Product.SkuMaxLength);

        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(item => item.UnitPriceSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.LineAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.ReturnRequestId, item.ProductId })
            .IsUnique();
    }
}
