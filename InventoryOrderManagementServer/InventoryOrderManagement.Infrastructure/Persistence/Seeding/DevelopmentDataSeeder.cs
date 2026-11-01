using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates a small demo catalog in Development so Categories, Products, Suppliers, and Customers flows can be exercised.
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

        await EnsureCustomerAsync(
            dbContext,
            customerType: CustomerType.Individual,
            name: "John Smith",
            taxIdentifier: "111.222.333-44",
            email: "john.smith@demo.local",
            phone: "+55 81 99999-8888",
            address: CustomerAddress.Create(
                "50000-000",
                "Example Street",
                "100",
                "Apartment 301",
                "Boa Viagem",
                "Recife",
                "PE",
                "Brazil"),
            deactivate: false,
            cancellationToken);

        await EnsureCustomerAsync(
            dbContext,
            customerType: CustomerType.Company,
            name: "Acme Retail Ltd.",
            taxIdentifier: "88.888.888/0001-88",
            email: "purchasing@acmeretail.demo",
            phone: "+55 11 3003-4004",
            address: CustomerAddress.Create(
                "01310-100",
                "Avenida Paulista",
                "1000",
                null,
                "Bela Vista",
                "Sao Paulo",
                "SP",
                "Brazil"),
            deactivate: false,
            cancellationToken);

        await EnsureCustomerAsync(
            dbContext,
            customerType: CustomerType.Individual,
            name: "Walk-in Guest",
            taxIdentifier: null,
            email: null,
            phone: "+55 81 98888-7777",
            address: null,
            deactivate: false,
            cancellationToken);

        await EnsureCustomerAsync(
            dbContext,
            customerType: CustomerType.Company,
            name: "Closed Store SA",
            taxIdentifier: "99.999.999/0001-99",
            email: "ops@closedstore.demo",
            phone: "+55 21 2222-1111",
            address: null,
            deactivate: true,
            cancellationToken);

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

        var product = Product.Create(sku, name, description, currentPrice, categoryId);
        dbContext.Products.Add(product);
        dbContext.InventoryBalances.Add(InventoryBalance.Create(product.Id));
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

    private static async Task EnsureCustomerAsync(
        InventoryDbContext dbContext,
        CustomerType customerType,
        string name,
        string? taxIdentifier,
        string? email,
        string? phone,
        CustomerAddress? address,
        bool deactivate,
        CancellationToken cancellationToken)
    {
        var normalizedTaxIdentifier = Customer.NormalizeTaxIdentifierForComparison(taxIdentifier);

        Customer? existing = null;
        if (normalizedTaxIdentifier is not null)
        {
            existing = await dbContext.Customers
                .FirstOrDefaultAsync(
                    customer => customer.NormalizedTaxIdentifier == normalizedTaxIdentifier,
                    cancellationToken);
        }
        else
        {
            existing = await dbContext.Customers
                .FirstOrDefaultAsync(customer => customer.Name == name.Trim(), cancellationToken);
        }

        if (existing is not null)
        {
            if (deactivate && existing.IsActive)
            {
                existing.Deactivate();
            }

            return;
        }

        var customer = Customer.Create(customerType, name, taxIdentifier, email, phone, address);

        if (deactivate)
        {
            customer.Deactivate();
        }

        dbContext.Customers.Add(customer);
    }
}
