using InventoryOrderManagement.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(role => role.Description)
            .HasMaxLength(ApplicationRole.DescriptionMaxLength);

        builder.Property(role => role.IsSystemRole)
            .IsRequired();
    }
}
