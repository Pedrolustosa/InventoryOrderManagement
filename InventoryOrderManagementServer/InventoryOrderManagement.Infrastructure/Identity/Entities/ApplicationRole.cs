using Microsoft.AspNetCore.Identity;

namespace InventoryOrderManagement.Infrastructure.Identity.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public const int DescriptionMaxLength = 300;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
