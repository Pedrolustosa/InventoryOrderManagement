using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.CustomerType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(customer => customer.Name)
            .IsRequired()
            .HasMaxLength(Customer.NameMaxLength);

        builder.Property(customer => customer.TaxIdentifier)
            .HasMaxLength(Customer.TaxIdentifierMaxLength);

        builder.Property(customer => customer.NormalizedTaxIdentifier)
            .HasMaxLength(Customer.TaxIdentifierMaxLength);

        builder.HasIndex(customer => customer.NormalizedTaxIdentifier)
            .IsUnique();

        builder.Property(customer => customer.Email)
            .HasMaxLength(Customer.EmailMaxLength);

        builder.Property(customer => customer.Phone)
            .HasMaxLength(Customer.PhoneMaxLength);

        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .IsRequired();

        builder.Property(customer => customer.UpdatedAtUtc);

        builder.OwnsOne(customer => customer.Address, address =>
        {
            address.Property(value => value.PostalCode)
                .HasColumnName("AddressPostalCode")
                .HasMaxLength(CustomerAddress.PostalCodeMaxLength);

            address.Property(value => value.Street)
                .HasColumnName("AddressStreet")
                .HasMaxLength(CustomerAddress.StreetMaxLength);

            address.Property(value => value.Number)
                .HasColumnName("AddressNumber")
                .HasMaxLength(CustomerAddress.NumberMaxLength);

            address.Property(value => value.Complement)
                .HasColumnName("AddressComplement")
                .HasMaxLength(CustomerAddress.ComplementMaxLength);

            address.Property(value => value.Neighborhood)
                .HasColumnName("AddressNeighborhood")
                .HasMaxLength(CustomerAddress.NeighborhoodMaxLength);

            address.Property(value => value.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(CustomerAddress.CityMaxLength);

            address.Property(value => value.State)
                .HasColumnName("AddressState")
                .HasMaxLength(CustomerAddress.StateMaxLength);

            address.Property(value => value.Country)
                .HasColumnName("AddressCountry")
                .HasMaxLength(CustomerAddress.CountryMaxLength);
        });

        builder.Navigation(customer => customer.Address)
            .IsRequired(false);
    }
}
