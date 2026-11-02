using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using InventoryOrderManagement.Infrastructure.Identity;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Populates Development data for every screen: catalog, stock, orders, purchases, reports, audit, and sign-in.
/// Idempotent: safe to run on every startup. Existing rows are left in place.
/// Demo passwords are development-only and are not applied again once the account exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string SeedCorrelationId = "development-seed";

    private static readonly DemoAccount[] DemoAccounts =
    [
        new("admin.demo@localhost", "Admin!Access123", "Demo Administrator", SystemRoles.Administrator, true),
        new("manager@localhost", "Manager!Access123", "Demo Manager", SystemRoles.Manager, true),
        new("inventory@localhost", "Inventory!Op123", "Demo Inventory", SystemRoles.InventoryOperator, true),
        new("sales@localhost", "Sales!Access123", "Demo Sales", SystemRoles.SalesOperator, true),
        new("viewer@localhost", "Viewer!Access123", "Demo Viewer", SystemRoles.Viewer, true),
        new("inactive@localhost", "Inactive!User123", "Inactive Viewer", SystemRoles.Viewer, false)
    ];

    public static async Task SeedAsync(
        InventoryDbContext dbContext,
        IServiceProvider? services = null,
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

        var keyboard = await EnsureProductAsync(
            dbContext,
            sku: "SEED-KB-001",
            name: "Mechanical Keyboard",
            description: "RGB switches, demo stock",
            currentPrice: 249.90m,
            categoryId: peripherals.Id,
            cancellationToken);

        var mouse = await EnsureProductAsync(
            dbContext,
            sku: "SEED-MS-001",
            name: "Wireless Mouse",
            description: "Ergonomic, demo stock",
            currentPrice: 99.90m,
            categoryId: peripherals.Id,
            cancellationToken);

        var notebook = await EnsureProductAsync(
            dbContext,
            sku: "SEED-NB-001",
            name: "A5 Notebook",
            description: "Ruled pages",
            currentPrice: 18.50m,
            categoryId: officeSupplies.Id,
            cancellationToken);

        var pen = await EnsureProductAsync(
            dbContext,
            sku: "SEED-PN-001",
            name: "Gel Pen Pack",
            description: "Black ink, 12 units",
            currentPrice: 24.00m,
            categoryId: officeSupplies.Id,
            cancellationToken);

        var genericItem = await EnsureProductAsync(
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

        var officeWholesale = await EnsureSupplierAsync(
            dbContext,
            name: "Office Wholesale Brasil",
            taxIdentifier: "66.666.666/0001-66",
            contactName: "Maria Souza",
            email: "sales@officewholesale.demo",
            phone: "+55 11 4002-8922",
            notes: "Stationery and desk materials.",
            deactivate: false,
            cancellationToken);

        var neighborhoodHardware = await EnsureSupplierAsync(
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

        _ = formerPartner;

        var johnSmith = await EnsureCustomerAsync(
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

        var acmeRetail = await EnsureCustomerAsync(
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

        await SeedOperationsAsync(
            dbContext,
            keyboard,
            mouse,
            notebook,
            pen,
            genericItem,
            techComponents,
            officeWholesale,
            neighborhoodHardware,
            johnSmith,
            acmeRetail,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (services is not null)
        {
            await SeedDemoAccessAsync(services, dbContext, cancellationToken);
        }
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

    private static async Task<Product> EnsureProductAsync(
        InventoryDbContext dbContext,
        string sku,
        string name,
        string? description,
        decimal currentPrice,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();

        var existing = await dbContext.Products
            .FirstOrDefaultAsync(product => product.Sku == normalizedSku, cancellationToken);

        if (existing is not null)
        {
            var hasBalance = await dbContext.InventoryBalances
                .AnyAsync(balance => balance.ProductId == existing.Id, cancellationToken);

            if (!hasBalance)
            {
                dbContext.InventoryBalances.Add(InventoryBalance.Create(existing.Id));
            }

            return existing;
        }

        var product = Product.Create(sku, name, description, currentPrice, categoryId);
        dbContext.Products.Add(product);
        dbContext.InventoryBalances.Add(InventoryBalance.Create(product.Id));
        return product;
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

    private static async Task<Customer> EnsureCustomerAsync(
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

            return existing;
        }

        var customer = Customer.Create(customerType, name, taxIdentifier, email, phone, address);

        if (deactivate)
        {
            customer.Deactivate();
        }

        dbContext.Customers.Add(customer);
        return customer;
    }

    private static async Task SeedOperationsAsync(
        InventoryDbContext dbContext,
        Product keyboard,
        Product mouse,
        Product notebook,
        Product pen,
        Product genericItem,
        Supplier techComponents,
        Supplier officeWholesale,
        Supplier neighborhoodHardware,
        Customer johnSmith,
        Customer acmeRetail,
        CancellationToken cancellationToken)
    {
        await EnsureStockEntryAsync(dbContext, keyboard, techComponents, 50, 10, cancellationToken);
        await EnsureStockEntryAsync(dbContext, mouse, techComponents, 30, 8, cancellationToken);
        await EnsureStockEntryAsync(dbContext, notebook, officeWholesale, 40, 5, cancellationToken);
        await EnsureStockEntryAsync(dbContext, pen, officeWholesale, 25, 30, cancellationToken);
        await EnsureMinimumStockAsync(dbContext, genericItem, 5, cancellationToken);

        await EnsurePurchaseAsync(
            dbContext,
            number: "SEED-PO-DRAFT",
            supplier: techComponents,
            product: keyboard,
            orderedQuantity: 10,
            unitCost: 180.00m,
            notes: "Waiting for the supplier confirmation.",
            send: false,
            quantityReceived: null,
            receiptNumber: null,
            cancelReason: null,
            cancellationToken);

        await EnsurePurchaseAsync(
            dbContext,
            number: "SEED-PO-SENT",
            supplier: officeWholesale,
            product: pen,
            orderedQuantity: 20,
            unitCost: 8.00m,
            notes: "Sent to the stationery supplier.",
            send: true,
            quantityReceived: null,
            receiptNumber: null,
            cancelReason: null,
            cancellationToken);

        await EnsurePurchaseAsync(
            dbContext,
            number: "SEED-PO-PART",
            supplier: neighborhoodHardware,
            product: notebook,
            orderedQuantity: 20,
            unitCost: 6.00m,
            notes: "First delivery of notebooks.",
            send: true,
            quantityReceived: 8,
            receiptNumber: "SEED-PR-PART",
            cancelReason: null,
            cancellationToken);

        await EnsurePurchaseAsync(
            dbContext,
            number: "SEED-PO-RECV",
            supplier: techComponents,
            product: mouse,
            orderedQuantity: 12,
            unitCost: 40.00m,
            notes: "Full mouse delivery.",
            send: true,
            quantityReceived: 12,
            receiptNumber: "SEED-PR-RECV",
            cancelReason: null,
            cancellationToken);

        await EnsurePurchaseAsync(
            dbContext,
            number: "SEED-PO-CANC",
            supplier: officeWholesale,
            product: genericItem,
            orderedQuantity: 4,
            unitCost: 3.00m,
            notes: "Quotation that was not accepted.",
            send: true,
            quantityReceived: null,
            receiptNumber: null,
            cancelReason: "The quotation expired.",
            cancellationToken);

        var johnAddress = ShippingAddressFor(johnSmith);
        var acmeAddress = ShippingAddressFor(acmeRetail);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-DRAFT",
            customer: johnSmith,
            shippingAddress: johnAddress,
            product: mouse,
            quantity: 2,
            progress: SalesProgress.Draft,
            cancelReason: null,
            cancellationToken);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-CONF",
            customer: acmeRetail,
            shippingAddress: acmeAddress,
            product: keyboard,
            quantity: 2,
            progress: SalesProgress.Confirmed,
            cancelReason: null,
            cancellationToken);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-SHIP",
            customer: acmeRetail,
            shippingAddress: acmeAddress,
            product: keyboard,
            quantity: 1,
            progress: SalesProgress.Shipped,
            cancelReason: null,
            cancellationToken);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-DEL",
            customer: johnSmith,
            shippingAddress: johnAddress,
            product: notebook,
            quantity: 3,
            progress: SalesProgress.Delivered,
            cancelReason: null,
            cancellationToken);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-CANC",
            customer: johnSmith,
            shippingAddress: johnAddress,
            product: pen,
            quantity: 1,
            progress: SalesProgress.CancelledDraft,
            cancelReason: "Customer changed their mind.",
            cancellationToken);

        await EnsureSalesOrderAsync(
            dbContext,
            number: "SEED-SO-CANC-CONF",
            customer: acmeRetail,
            shippingAddress: acmeAddress,
            product: pen,
            quantity: 4,
            progress: SalesProgress.CancelledConfirmed,
            cancelReason: "Payment was not approved.",
            cancellationToken);
    }

    private static async Task EnsureStockEntryAsync(
        InventoryDbContext dbContext,
        Product product,
        Supplier supplier,
        int quantity,
        int minimumStockLevel,
        CancellationToken cancellationToken)
    {
        var reason = $"Seed stock entry {product.Sku}.";
        if (await dbContext.StockMovements.AnyAsync(movement => movement.Reason == reason, cancellationToken))
        {
            return;
        }

        var balance = await TrackedBalanceAsync(dbContext, product.Id, cancellationToken);
        var previousQuantity = balance.QuantityOnHand;
        balance.Receive(quantity);

        if (balance.MinimumStockLevel == 0 && minimumStockLevel > 0)
        {
            balance.SetMinimumStockLevel(minimumStockLevel);
        }

        dbContext.StockMovements.Add(StockMovement.Create(
            product.Id,
            supplier.Id,
            StockMovementType.Entry,
            quantity,
            previousQuantity,
            balance.QuantityOnHand,
            reason));
    }

    private static async Task EnsureMinimumStockAsync(
        InventoryDbContext dbContext,
        Product product,
        int minimumStockLevel,
        CancellationToken cancellationToken)
    {
        var balance = await TrackedBalanceAsync(dbContext, product.Id, cancellationToken);
        if (balance.MinimumStockLevel == 0)
        {
            balance.SetMinimumStockLevel(minimumStockLevel);
        }
    }

    private static async Task EnsurePurchaseAsync(
        InventoryDbContext dbContext,
        string number,
        Supplier supplier,
        Product product,
        int orderedQuantity,
        decimal unitCost,
        string notes,
        bool send,
        int? quantityReceived,
        string? receiptNumber,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        if (await dbContext.PurchaseOrders.AnyAsync(order => order.PurchaseOrderNumber == number, cancellationToken))
        {
            return;
        }

        var item = PurchaseOrderItem.Create(product.Id, product.Sku, product.Name, orderedQuantity, unitCost);
        var order = PurchaseOrder.Create(number, supplier.Id, supplier.Name, notes, [item]);
        dbContext.PurchaseOrders.Add(order);

        if (send)
        {
            order.Send();
        }

        if (quantityReceived is int received)
        {
            order.Receive([(item.Id, received)]);

            var receipt = PurchaseReceipt.Create(
                receiptNumber ?? throw new InvalidOperationException("A received purchase seed requires a receipt number."),
                order.Id,
                notes,
                [
                    PurchaseReceiptItem.Create(
                        item.Id,
                        item.ProductId,
                        item.ProductSku,
                        item.ProductName,
                        received,
                        item.UnitCost)
                ]);

            dbContext.PurchaseReceipts.Add(receipt);

            var balance = await TrackedBalanceAsync(dbContext, product.Id, cancellationToken);
            var previousQuantity = balance.QuantityOnHand;
            balance.Receive(received);
            dbContext.StockMovements.Add(StockMovement.CreateForPurchase(
                product.Id,
                supplier.Id,
                order.Id,
                receipt.Id,
                received,
                previousQuantity,
                balance.QuantityOnHand,
                notes));
        }

        if (cancelReason is not null)
        {
            order.Cancel(cancelReason);
        }
    }

    private static async Task EnsureSalesOrderAsync(
        InventoryDbContext dbContext,
        string number,
        Customer customer,
        OrderShippingAddress shippingAddress,
        Product product,
        int quantity,
        SalesProgress progress,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Orders.AnyAsync(order => order.OrderNumber == number, cancellationToken))
        {
            return;
        }

        var order = Order.Create(
            number,
            customer.Id,
            customer.Name,
            shippingAddress,
            [OrderItem.Create(product.Id, product.Sku, product.Name, quantity, product.CurrentPrice)]);

        dbContext.Orders.Add(order);

        if (progress is SalesProgress.Draft or SalesProgress.CancelledDraft)
        {
            if (progress == SalesProgress.CancelledDraft)
            {
                order.Cancel(cancelReason ?? throw new InvalidOperationException("A cancelled order seed requires a reason."));
            }

            return;
        }

        await IssueForOrderAsync(dbContext, order, cancellationToken);
        order.Confirm();

        if (progress == SalesProgress.CancelledConfirmed)
        {
            await ReturnForOrderAsync(dbContext, order, cancellationToken);
            order.Cancel(cancelReason ?? throw new InvalidOperationException("A cancelled order seed requires a reason."));
            return;
        }

        if (progress is SalesProgress.Shipped or SalesProgress.Delivered)
        {
            order.Ship();
        }

        if (progress == SalesProgress.Delivered)
        {
            order.Deliver();
        }
    }

    private static async Task IssueForOrderAsync(
        InventoryDbContext dbContext,
        Order order,
        CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var balance = await TrackedBalanceAsync(dbContext, item.ProductId, cancellationToken);
            var previousQuantity = balance.QuantityOnHand;
            balance.Issue(item.Quantity);
            dbContext.StockMovements.Add(StockMovement.CreateForOrder(
                item.ProductId,
                order.Id,
                StockMovementType.OrderExit,
                item.Quantity,
                previousQuantity,
                balance.QuantityOnHand,
                $"Stock issued for order {order.OrderNumber}."));
        }
    }

    private static async Task ReturnForOrderAsync(
        InventoryDbContext dbContext,
        Order order,
        CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var balance = await TrackedBalanceAsync(dbContext, item.ProductId, cancellationToken);
            var previousQuantity = balance.QuantityOnHand;
            balance.Receive(item.Quantity);
            dbContext.StockMovements.Add(StockMovement.CreateForOrder(
                item.ProductId,
                order.Id,
                StockMovementType.OrderCancellationReturn,
                item.Quantity,
                previousQuantity,
                balance.QuantityOnHand,
                $"Stock returned for cancelled order {order.OrderNumber}."));
        }
    }

    private static async Task<InventoryBalance> TrackedBalanceAsync(
        InventoryDbContext dbContext,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InventoryBalances
            .FirstAsync(balance => balance.ProductId == productId, cancellationToken);
    }

    private static OrderShippingAddress ShippingAddressFor(Customer customer)
    {
        if (customer.Address is CustomerAddress address)
        {
            return OrderShippingAddress.FromCustomerAddress(address);
        }

        return OrderShippingAddress.Create(
            "50000-000",
            "Example Street",
            "100",
            null,
            "Boa Viagem",
            "Recife",
            "PE",
            "Brazil");
    }

    private static async Task SeedDemoAccessAsync(
        IServiceProvider services,
        InventoryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        Guid? administratorId = null;

        foreach (var account in DemoAccounts)
        {
            if (await roleManager.FindByNameAsync(account.Role) is null)
            {
                continue;
            }

            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = account.Email,
                    Email = account.Email,
                    EmailConfirmed = true,
                    DisplayName = account.DisplayName,
                    IsActive = account.IsActive,
                    MustChangePassword = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var created = await userManager.CreateAsync(user, account.Password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Demo account '{account.Email}' could not be created: " +
                        string.Join(" ", created.Errors.Select(error => error.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, account.Role))
            {
                var assignment = await userManager.AddToRoleAsync(user, account.Role);
                if (!assignment.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Demo account '{account.Email}' could not be assigned to '{account.Role}'.");
                }
            }

            if (account.Role == SystemRoles.Administrator)
            {
                administratorId = user.Id;
            }
        }

        if (await dbContext.SecurityAuditLogs.AnyAsync(log => log.CorrelationId == SeedCorrelationId, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = administratorId,
            TargetUserId = administratorId,
            Action = SecurityActions.LoginSucceeded,
            EntityType = SecurityEntities.Session,
            EntityId = administratorId?.ToString(),
            Outcome = SecurityOutcomes.Success,
            OccurredAtUtc = now.AddMinutes(-30),
            CorrelationId = SeedCorrelationId,
            Details = "Development seed sign-in for Demo Administrator."
        });
        dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = administratorId,
            TargetUserId = administratorId,
            Action = SecurityActions.UserCreated,
            EntityType = SecurityEntities.User,
            EntityId = administratorId?.ToString(),
            Outcome = SecurityOutcomes.Success,
            OccurredAtUtc = now.AddMinutes(-40),
            CorrelationId = SeedCorrelationId,
            Details = "Development seed created the demo sign-in accounts."
        });
        dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = null,
            TargetUserId = null,
            Action = SecurityActions.LoginFailed,
            EntityType = SecurityEntities.Session,
            EntityId = "unknown@localhost",
            Outcome = SecurityOutcomes.Failure,
            OccurredAtUtc = now.AddMinutes(-20),
            CorrelationId = SeedCorrelationId,
            Details = "Development seed sample of a rejected sign-in."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoAccount(
        string Email,
        string Password,
        string DisplayName,
        string Role,
        bool IsActive);

    private enum SalesProgress
    {
        Draft,
        Confirmed,
        Shipped,
        Delivered,
        CancelledDraft,
        CancelledConfirmed
    }
}
