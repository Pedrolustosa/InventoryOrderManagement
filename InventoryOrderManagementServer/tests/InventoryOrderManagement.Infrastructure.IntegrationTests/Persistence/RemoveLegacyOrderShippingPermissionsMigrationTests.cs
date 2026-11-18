using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class RemoveLegacyOrderShippingPermissionsMigrationTests : IDisposable
{
    private const string PreviousMigration = "20261108042259_AddReturnStockFinanceReversal";
    private const string PermissionClaimType = "permission";
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"iom-mig-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Up_RemovesOnlyTheLegacyShippingPermissions_FromEveryRole()
    {
        var customRole = Guid.NewGuid();
        var managerRole = Guid.NewGuid();

        await using (var before = CreateContext())
        {
            await before.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            before.Roles.AddRange(Role(customRole, "Dispatch Desk"), Role(managerRole, "Manager"));
            before.RoleClaims.AddRange(
                Claim(customRole, "Orders.View"),
                Claim(customRole, "Orders.Ship"),
                Claim(customRole, "Orders.Deliver"),
                Claim(customRole, "Shipments.Dispatch"),
                Claim(managerRole, "Orders.Ship"),
                Claim(managerRole, "Orders.Cancel"),
                // Same value under another claim type is not a permission and must survive.
                new IdentityRoleClaim<Guid> { RoleId = managerRole, ClaimType = "note", ClaimValue = "Orders.Ship" });
            await before.SaveChangesAsync();
        }

        await using (var after = CreateContext())
        {
            await after.Database.MigrateAsync();

            Assert.Equal(
                ["Orders.View", "Shipments.Dispatch"],
                await PermissionsOf(after, customRole));
            Assert.Equal(["Orders.Cancel"], await PermissionsOf(after, managerRole));
            Assert.True(await after.RoleClaims.AnyAsync(claim => claim.ClaimType == "note" && claim.ClaimValue == "Orders.Ship"));
            Assert.Equal(2, await after.Roles.CountAsync());
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private InventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite($"Data Source={_databasePath}").Options);

    private static ApplicationRole Role(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static IdentityRoleClaim<Guid> Claim(Guid roleId, string permission) =>
        new() { RoleId = roleId, ClaimType = PermissionClaimType, ClaimValue = permission };

    private static async Task<List<string?>> PermissionsOf(InventoryDbContext dbContext, Guid roleId) =>
        await dbContext.RoleClaims
            .Where(claim => claim.RoleId == roleId && claim.ClaimType == PermissionClaimType)
            .Select(claim => claim.ClaimValue)
            .OrderBy(value => value)
            .ToListAsync();
}
