using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequests");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ReturnNumber)
            .IsRequired()
            .HasMaxLength(ReturnRequest.ReturnNumberMaxLength);

        builder.HasIndex(entity => entity.ReturnNumber)
            .IsUnique();

        builder.Property(entity => entity.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(entity => entity.Type);

        builder.Property(entity => entity.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(entity => entity.Status);

        builder.HasIndex(entity => entity.CustomerId);
        builder.HasIndex(entity => entity.SupplierId);
        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.PurchaseOrderId);
        builder.HasIndex(entity => entity.CreatedAtUtc);

        builder.Property(entity => entity.CustomerName)
            .HasMaxLength(200);

        builder.Property(entity => entity.SupplierName)
            .HasMaxLength(200);

        builder.Property(entity => entity.SourceReference)
            .HasMaxLength(ReturnRequest.SourceReferenceMaxLength);

        builder.Property(entity => entity.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(entity => entity.ReasonDescription)
            .HasMaxLength(ReturnRequest.ReasonDescriptionMaxLength);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(ReturnRequest.NotesMaxLength);

        builder.Property(entity => entity.EstimatedAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(entity => entity.FinancialTreatment)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(entity => entity.FinancialStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(entity => entity.EstimatedFinancialAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(entity => entity.FinancialNotes)
            .HasMaxLength(ReturnRequest.FinancialNotesMaxLength);

        builder.Property(entity => entity.AuthorizationReference)
            .HasMaxLength(ReturnRequest.AuthorizationReferenceMaxLength);

        builder.Property(entity => entity.DispatchReference)
            .HasMaxLength(ReturnRequest.DispatchReferenceMaxLength);

        builder.Property(entity => entity.CancellationReason)
            .HasMaxLength(ReturnRequest.CancellationReasonMaxLength);

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(entity => entity.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(entity => entity.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Items)
            .WithOne()
            .HasForeignKey(item => item.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(entity => entity.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
