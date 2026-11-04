using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.PurchaseOrderNumber)
            .IsRequired()
            .HasMaxLength(PurchaseOrder.PurchaseOrderNumberMaxLength);

        builder.HasIndex(order => order.PurchaseOrderNumber)
            .IsUnique();

        builder.Property(order => order.SupplierId)
            .IsRequired();

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(order => order.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => order.SupplierId);

        builder.Property(order => order.SupplierName)
            .IsRequired()
            .HasMaxLength(Supplier.NameMaxLength);

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(order => order.Status);

        builder.Property(order => order.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(order => order.Notes)
            .HasMaxLength(PurchaseOrder.NotesMaxLength);

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(order => order.CreatedAtUtc);

        builder.Property(order => order.CancellationReason)
            .HasMaxLength(PurchaseOrder.CancellationReasonMaxLength);

        builder.Property(order => order.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
