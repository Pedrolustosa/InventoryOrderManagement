using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .IsRequired()
            .HasMaxLength(Order.OrderNumberMaxLength);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.CustomerId)
            .IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => order.CustomerId);

        builder.Property(order => order.CustomerName)
            .IsRequired()
            .HasMaxLength(Customer.NameMaxLength);

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(order => order.Status);

        builder.Property(order => order.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(order => order.CreatedAtUtc);

        builder.Property(order => order.CancellationReason)
            .HasMaxLength(Order.CancellationReasonMaxLength);

        builder.Property(order => order.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(order => order.ShippingAddress, address =>
        {
            address.Property(value => value.PostalCode)
                .HasColumnName("ShippingPostalCode")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.PostalCodeMaxLength);

            address.Property(value => value.Street)
                .HasColumnName("ShippingStreet")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.StreetMaxLength);

            address.Property(value => value.Number)
                .HasColumnName("ShippingNumber")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.NumberMaxLength);

            address.Property(value => value.Complement)
                .HasColumnName("ShippingComplement")
                .HasMaxLength(OrderShippingAddress.ComplementMaxLength);

            address.Property(value => value.Neighborhood)
                .HasColumnName("ShippingNeighborhood")
                .HasMaxLength(OrderShippingAddress.NeighborhoodMaxLength);

            address.Property(value => value.City)
                .HasColumnName("ShippingCity")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.CityMaxLength);

            address.Property(value => value.State)
                .HasColumnName("ShippingState")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.StateMaxLength);

            address.Property(value => value.Country)
                .HasColumnName("ShippingCountry")
                .IsRequired()
                .HasMaxLength(OrderShippingAddress.CountryMaxLength);
        });

        builder.Navigation(order => order.ShippingAddress)
            .IsRequired();
    }
}
