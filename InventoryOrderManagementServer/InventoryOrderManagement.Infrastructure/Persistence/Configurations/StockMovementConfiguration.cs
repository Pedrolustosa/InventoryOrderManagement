using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint(
                "CK_StockMovements_Quantity",
                "\"Quantity\" > 0");
            table.HasCheckConstraint(
                "CK_StockMovements_PreviousQuantity",
                "\"PreviousQuantity\" >= 0");
            table.HasCheckConstraint(
                "CK_StockMovements_NewQuantity",
                "\"NewQuantity\" >= 0");
        });

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.ProductId)
            .IsRequired();

        builder.Property(movement => movement.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(movement => movement.Quantity)
            .IsRequired();

        builder.Property(movement => movement.PreviousQuantity)
            .IsRequired();

        builder.Property(movement => movement.NewQuantity)
            .IsRequired();

        builder.Property(movement => movement.Reason)
            .HasMaxLength(StockMovement.ReasonMaxLength);

        builder.Property(movement => movement.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(movement => movement.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(movement => movement.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(movement => new { movement.ProductId, movement.CreatedAtUtc });
        builder.HasIndex(movement => movement.SupplierId);
        builder.HasIndex(movement => movement.CreatedAtUtc);
        builder.HasIndex(movement => movement.OrderId);
    }
}
