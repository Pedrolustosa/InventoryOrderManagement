using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ToTable("PurchaseReceipts");

        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(PurchaseReceipt.ReceiptNumberMaxLength);

        builder.HasIndex(receipt => receipt.ReceiptNumber)
            .IsUnique();

        builder.Property(receipt => receipt.PurchaseOrderId)
            .IsRequired();

        builder.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(receipt => receipt.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(receipt => receipt.PurchaseOrderId);

        builder.Property(receipt => receipt.ReceivedAtUtc)
            .IsRequired();

        builder.Property(receipt => receipt.Notes)
            .HasMaxLength(PurchaseReceipt.NotesMaxLength);

        builder.HasMany(receipt => receipt.Items)
            .WithOne()
            .HasForeignKey(item => item.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(receipt => receipt.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
