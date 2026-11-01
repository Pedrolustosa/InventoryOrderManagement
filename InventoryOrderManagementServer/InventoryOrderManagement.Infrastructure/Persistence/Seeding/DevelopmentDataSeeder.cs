using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates a small demo catalog in Development so Categories, Products, and Suppliers flows can be exercised.
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

        var techComponents = await EnsureSupplierAsync(
            dbContext,
            name: "Tech Components Ltd.",
            taxIdentifier: "55.555.555/0001-55",
            contactName: "John Smith",
            email: "contact@techcomponents.demo",
            phone: "+55 81 3333-4444",
            notes: "Primary seed supplier of computer components.",
            deactivate: false,
            cancellationToken);

        await EnsureSupplierAsync(
            dbContext,
            name: "Office Wholesale Brasil",
            taxIdentifier: "66.666.666/0001-66",
            contactName: "Maria Souza",
            email: "sales@officewholesale.demo",
            phone: "+55 11 4002-8922",
            notes: "Stationery and desk materials.",
            deactivate: false,
            cancellationToken);

        await EnsureSupplierAsync(
            dbContext,
            name: "Neighborhood Hardware",
            taxIdentifier: null,
            contactName: "Carlos Lima",
            email: null,
            phone: "+55 81 98888-0000",
            notes: "Local supplier without tax identifier.",
            deactivate: false,
            cancellationToken);

        var formerPartner = await EnsureSupplierAsync(
            dbContext,
            name: "Former Partner Supply",
            taxIdentifier: "77.777.777/0001-77",
            contactName: "Ana Costa",
            email: "ops@formerpartner.demo",
            phone: "+55 21 2222-3333",
            notes: "Inactive supplier for status filter scenarios.",
            deactivate: true,
            cancellationToken);

        _ = techComponents;
        _ = formerPartner;

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

    private static async Task<Supplier> EnsureSupplierAsync(
        InventoryDbContext dbContext,
        string name,
        string? taxIdentifier,
        string? contactName,
        string? email,
        string? phone,
        string? notes,
        bool deactivate,
        CancellationToken cancellationToken)
    {
        var normalizedTaxIdentifier = Supplier.NormalizeTaxIdentifierForComparison(taxIdentifier);

        Supplier? existing = null;
        if (normalizedTaxIdentifier is not null)
        {
            existing = await dbContext.Suppliers
                .FirstOrDefaultAsync(
                    supplier => supplier.NormalizedTaxIdentifier == normalizedTaxIdentifier,
                    cancellationToken);
        }
        else
        {
            existing = await dbContext.Suppliers
                .FirstOrDefaultAsync(supplier => supplier.Name == name.Trim(), cancellationToken);
        }

        if (existing is not null)
        {
            if (deactivate && existing.IsActive)
            {
                existing.Deactivate();
            }

            return existing;
        }

        var supplier = Supplier.Create(name, taxIdentifier, contactName, email, phone, notes);

        if (deactivate)
        {
            supplier.Deactivate();
        }

        dbContext.Suppliers.Add(supplier);
        return supplier;
    }
}
