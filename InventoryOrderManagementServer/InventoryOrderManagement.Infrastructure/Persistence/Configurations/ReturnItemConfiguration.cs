using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable("ReturnItems", table =>
        {
            table.HasCheckConstraint("CK_ReturnItems_RequestedQuantity", "\"RequestedQuantity\" > 0");
            table.HasCheckConstraint("CK_ReturnItems_ReceivedQuantity", "\"ReceivedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_DispatchedQuantity", "\"DispatchedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_RestockedQuantity", "\"RestockedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_QuarantinedQuantity", "\"QuarantinedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_DiscardedQuantity", "\"DiscardedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_RejectedQuantity", "\"RejectedQuantity\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_UnitValueSnapshot", "\"UnitValueSnapshot\" >= 0");
            table.HasCheckConstraint("CK_ReturnItems_LineEstimatedAmount", "\"LineEstimatedAmount\" >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ReturnRequestId)
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

        builder.Property(item => item.SourceQuantity)
            .IsRequired();

        builder.Property(item => item.PreviouslyReturnedQuantity)
            .IsRequired();

        builder.Property(item => item.RequestedQuantity)
            .IsRequired();

        builder.Property(item => item.ReceivedQuantity)
            .IsRequired();

        builder.Property(item => item.DispatchedQuantity)
            .IsRequired();

        builder.Property(item => item.RestockedQuantity)
            .IsRequired();

        builder.Property(item => item.QuarantinedQuantity)
            .IsRequired();

        builder.Property(item => item.DiscardedQuantity)
            .IsRequired();

        builder.Property(item => item.RejectedQuantity)
            .IsRequired();

        builder.Property(item => item.UnitValueSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.LineEstimatedAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.Condition)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(item => item.InspectionNotes)
            .HasMaxLength(ReturnItem.InspectionNotesMaxLength);

        builder.HasIndex(item => new { item.ReturnRequestId, item.ProductId })
            .IsUnique();

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.OrderItemId);
        builder.HasIndex(item => item.PurchaseOrderItemId);
    }
}
