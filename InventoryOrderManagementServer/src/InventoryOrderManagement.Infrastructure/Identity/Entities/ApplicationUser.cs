using Microsoft.AspNetCore.Identity;

namespace InventoryOrderManagement.Infrastructure.Identity.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 150;

    public const int PhoneNumberMaxLength = 30;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}
