using System.Security.Claims;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Infrastructure.Identity;

/// <summary>
/// Creates the system roles and, when configured, the first administrator.
/// Bootstrap credentials come only from configuration (User Secrets or environment variables):
/// Identity:Bootstrap:Email, Identity:Bootstrap:Password, Identity:Bootstrap:DisplayName.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        foreach (var roleName in SystemRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    Description = DescriptionFor(roleName),
                    IsSystemRole = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var created = await roleManager.CreateAsync(role);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"System role '{roleName}' could not be created: " +
                        string.Join(" ", created.Errors.Select(error => error.Code)));
                }
            }

            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(claim => claim.Type == Permissions.ClaimType))
            {
                continue;
            }

            foreach (var permission in RolePermissionDefaults.For(roleName))
            {
                var added = await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
                if (!added.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Permission '{permission}' could not be added to '{roleName}'.");
                }
            }
        }

        if (await userManager.GetUsersInRoleAsync(SystemRoles.Administrator) is { Count: > 0 })
        {
            return;
        }

        var email = configuration["Identity:Bootstrap:Email"];
        var password = configuration["Identity:Bootstrap:Password"];
        var displayName = configuration["Identity:Bootstrap:DisplayName"];
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(displayName))
        {
            logger.LogWarning(
                "No administrator exists. Set Identity:Bootstrap:Email, Identity:Bootstrap:Password and Identity:Bootstrap:DisplayName in User Secrets or environment variables to create the first account. Example: dotnet user-secrets set \"Identity:Bootstrap:Email\" \"admin@example.com\" --project InventoryOrderManagement.Api");
            return;
        }

        var trimmedName = displayName.Trim();
        if (trimmedName.Length == 0 || trimmedName.Length > ApplicationUser.DisplayNameMaxLength)
        {
            throw new InvalidOperationException(
                "Identity:Bootstrap:DisplayName must contain between 1 and 150 characters.");
        }

        var administrator = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            DisplayName = trimmedName,
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(administrator, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "The bootstrap administrator could not be created: " +
                string.Join(" ", result.Errors.Select(error => error.Code)));
        }

        var assignment = await userManager.AddToRoleAsync(administrator, SystemRoles.Administrator);
        if (!assignment.Succeeded)
        {
            await userManager.DeleteAsync(administrator);
            throw new InvalidOperationException(
                "The bootstrap administrator could not be assigned to the Administrator role.");
        }

        logger.LogInformation("Bootstrap administrator {Email} was created and must change the password at first sign-in.", administrator.Email);
    }

    private static string DescriptionFor(string roleName) => roleName switch
    {
        SystemRoles.Administrator => "Full access, including users, roles, and the security audit.",
        SystemRoles.Manager => "Operational management of categories, suppliers, customers, orders, and reports.",
        SystemRoles.InventoryOperator => "Stock operations and consultation of products and suppliers.",
        SystemRoles.SalesOperator => "Sales order lifecycle and consultation of products and customers.",
        SystemRoles.Viewer => "Read-only access to operational data and reports.",
        _ => string.Empty
    };
}
