using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Sku)
            .IsRequired()
            .HasMaxLength(Product.SkuMaxLength);

        builder.HasIndex(product => product.Sku)
            .IsUnique();

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(product => product.Description)
            .HasMaxLength(Product.DescriptionMaxLength);

        builder.Property(product => product.CurrentPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(product => product.IsActive)
            .IsRequired();

        builder.Property(product => product.CreatedAtUtc)
            .IsRequired();
    }
}
