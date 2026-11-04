using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public static readonly Guid UncategorizedCategoryId =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        builder.Property(category => category.NormalizedName)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        builder.HasIndex(category => category.NormalizedName)
            .IsUnique();

        builder.Property(category => category.Description)
            .HasMaxLength(Category.DescriptionMaxLength);

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.Property(category => category.UpdatedAtUtc);

        builder.HasData(new
        {
            Id = UncategorizedCategoryId,
            Name = "Uncategorized",
            NormalizedName = "UNCATEGORIZED",
            Description = (string?)null,
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = (DateTime?)null
        });
    }
}
