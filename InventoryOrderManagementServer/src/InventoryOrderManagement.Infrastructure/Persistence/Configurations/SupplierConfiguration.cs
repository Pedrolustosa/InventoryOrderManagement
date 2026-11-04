using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Name)
            .IsRequired()
            .HasMaxLength(Supplier.NameMaxLength);

        builder.Property(supplier => supplier.TaxIdentifier)
            .HasMaxLength(Supplier.TaxIdentifierMaxLength);

        builder.Property(supplier => supplier.NormalizedTaxIdentifier)
            .HasMaxLength(Supplier.TaxIdentifierMaxLength);

        builder.HasIndex(supplier => supplier.NormalizedTaxIdentifier)
            .IsUnique();

        builder.Property(supplier => supplier.ContactName)
            .HasMaxLength(Supplier.ContactNameMaxLength);

        builder.Property(supplier => supplier.Email)
            .HasMaxLength(Supplier.EmailMaxLength);

        builder.Property(supplier => supplier.Phone)
            .HasMaxLength(Supplier.PhoneMaxLength);

        builder.Property(supplier => supplier.Notes)
            .HasMaxLength(Supplier.NotesMaxLength);

        builder.Property(supplier => supplier.IsActive)
            .IsRequired();

        builder.Property(supplier => supplier.CreatedAtUtc)
            .IsRequired();

        builder.Property(supplier => supplier.UpdatedAtUtc);
    }
}
