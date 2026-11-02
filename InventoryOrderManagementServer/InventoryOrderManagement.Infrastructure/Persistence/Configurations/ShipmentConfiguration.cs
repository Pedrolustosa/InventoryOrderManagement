using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.OrderId)
            .IsRequired();

        builder.HasIndex(entity => entity.OrderId)
            .IsUnique();

        builder.Property(entity => entity.OrderNumber)
            .IsRequired()
            .HasMaxLength(Shipment.OrderNumberMaxLength);

        builder.Property(entity => entity.CustomerId)
            .IsRequired();

        builder.HasIndex(entity => entity.CustomerId);

        builder.Property(entity => entity.CustomerName)
            .IsRequired()
            .HasMaxLength(Shipment.CustomerNameMaxLength);

        builder.Property(entity => entity.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.CreatedAtUtc);

        builder.Property(entity => entity.CarrierName)
            .HasMaxLength(Shipment.CarrierNameMaxLength);

        builder.Property(entity => entity.TrackingCode)
            .HasMaxLength(Shipment.TrackingCodeMaxLength);

        builder.HasIndex(entity => entity.TrackingCode);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(Shipment.NotesMaxLength);

        builder.Property(entity => entity.FailureReason)
            .HasMaxLength(Shipment.FailureReasonMaxLength);

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.History)
            .WithOne()
            .HasForeignKey(entry => entry.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(entity => entity.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ShipmentStatusHistoryConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistories");

        builder.HasKey(entity => entity.Id);

        // Client-generated Guid; required so appending history after insert does not
        // hit EF Core's ValueGeneratedOnAdd concurrency false positive on SQLite.
        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.ShipmentId)
            .IsRequired();

        builder.HasIndex(entity => entity.ShipmentId);
        builder.HasIndex(entity => entity.OccurredAtUtc);

        builder.Property(entity => entity.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(entity => entity.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(ShipmentStatusHistory.NotesMaxLength);

        builder.Property(entity => entity.OccurredAtUtc)
            .IsRequired();
    }
}
