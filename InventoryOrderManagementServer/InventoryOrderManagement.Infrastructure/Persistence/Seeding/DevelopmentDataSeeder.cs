using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates a small demo catalog in Development so Categories and Products flows can be exercised.
/// Idempotent: safe to run on every startup.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        InventoryDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var peripherals = await EnsureCategoryAsync(
            dbContext,
            "Peripherals",
            "Keyboards, mice, and input devices",
            cancellationToken);

        var officeSupplies = await EnsureCategoryAsync(
            dbContext,
            "Office Supplies",
            "Everyday desk and office materials",
            cancellationToken);

        var housewares = await EnsureCategoryAsync(
            dbContext,
            "Housewares",
            "Empty category for deactivate success scenarios",
            cancellationToken);

        var archivedPromo = await EnsureCategoryAsync(
            dbContext,
            "Archived Promo",
            "Inactive category for status filter scenarios",
            cancellationToken);

        if (archivedPromo.IsActive)
        {
            archivedPromo.Deactivate();
        }

        await EnsureProductAsync(
            dbContext,
            sku: "SEED-KB-001",
            name: "Mechanical Keyboard",
            description: "RGB switches, demo stock",
            currentPrice: 249.90m,
            categoryId: peripherals.Id,
            cancellationToken);

        await EnsureProductAsync(
            dbContext,
            sku: "SEED-MS-001",
            name: "Wireless Mouse",
            description: "Ergonomic, demo stock",
            currentPrice: 99.90m,
            categoryId: peripherals.Id,
            cancellationToken);

        await EnsureProductAsync(
            dbContext,
            sku: "SEED-NB-001",
            name: "A5 Notebook",
            description: "Ruled pages",
            currentPrice: 18.50m,
            categoryId: officeSupplies.Id,
            cancellationToken);

        await EnsureProductAsync(
            dbContext,
            sku: "SEED-PN-001",
            name: "Gel Pen Pack",
            description: "Black ink, 12 units",
            currentPrice: 24.00m,
            categoryId: officeSupplies.Id,
            cancellationToken);

        await EnsureProductAsync(
            dbContext,
            sku: "SEED-UNC-001",
            name: "Generic Item",
            description: "Assigned to Uncategorized",
            currentPrice: 9.99m,
            categoryId: CategoryConfiguration.UncategorizedCategoryId,
            cancellationToken);

        // housewares intentionally left without products (deactivate without 409).
        _ = housewares;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Category> EnsureCategoryAsync(
        InventoryDbContext dbContext,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        var normalizedName = Category.NormalizeNameForComparison(name);

        var existing = await dbContext.Categories
            .FirstOrDefaultAsync(category => category.NormalizedName == normalizedName, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var category = Category.Create(name, description);
        dbContext.Categories.Add(category);
        return category;
    }

    private static async Task EnsureProductAsync(
        InventoryDbContext dbContext,
        string sku,
        string name,
        string? description,
        decimal currentPrice,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();

        var exists = await dbContext.Products
            .AnyAsync(product => product.Sku == normalizedSku, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Products.Add(Product.Create(sku, name, description, currentPrice, categoryId));
    }
}
