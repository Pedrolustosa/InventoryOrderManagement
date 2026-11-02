using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Infrastructure.Identity;

internal static class RolePermissionDefaults
{
    private static readonly string[] Manager =
    [
        Permissions.Products.View,
        Permissions.Categories.View,
        Permissions.Categories.Create,
        Permissions.Categories.Update,
        Permissions.Categories.Deactivate,
        Permissions.Suppliers.View,
        Permissions.Suppliers.Create,
        Permissions.Suppliers.Update,
        Permissions.Suppliers.Deactivate,
        Permissions.Customers.View,
        Permissions.Customers.Create,
        Permissions.Customers.Update,
        Permissions.Customers.Deactivate,
        Permissions.Inventory.View,
        Permissions.Orders.View,
        Permissions.Orders.Create,
        Permissions.Orders.UpdateDraft,
        Permissions.Orders.Confirm,
        Permissions.Orders.Ship,
        Permissions.Orders.Deliver,
        Permissions.Orders.Cancel,
        Permissions.Reports.View,
        Permissions.Purchases.View,
        Permissions.Purchases.Create,
        Permissions.Purchases.UpdateDraft,
        Permissions.Purchases.Send,
        Permissions.Purchases.Receive,
        Permissions.Purchases.Cancel
    ];

    private static readonly string[] InventoryOperator =
    [
        Permissions.Products.View,
        Permissions.Suppliers.View,
        Permissions.Inventory.View,
        Permissions.Inventory.Receive,
        Permissions.Inventory.Exit,
        Permissions.Inventory.Adjust,
        Permissions.Inventory.MinimumStockUpdate,
        Permissions.Purchases.View,
        Permissions.Purchases.Receive
    ];

    private static readonly string[] SalesOperator =
    [
        Permissions.Products.View,
        Permissions.Customers.View,
        Permissions.Orders.View,
        Permissions.Orders.Create,
        Permissions.Orders.UpdateDraft,
        Permissions.Orders.Confirm,
        Permissions.Orders.Ship,
        Permissions.Orders.Deliver,
        Permissions.Orders.Cancel
    ];

    private static readonly string[] Viewer =
    [
        Permissions.Products.View,
        Permissions.Categories.View,
        Permissions.Suppliers.View,
        Permissions.Customers.View,
        Permissions.Inventory.View,
        Permissions.Orders.View,
        Permissions.Reports.View,
        Permissions.Purchases.View
    ];

    public static IReadOnlyList<string> For(string roleName) => roleName switch
    {
        SystemRoles.Administrator => Permissions.All,
        SystemRoles.Manager => Manager,
        SystemRoles.InventoryOperator => InventoryOperator,
        SystemRoles.SalesOperator => SalesOperator,
        SystemRoles.Viewer => Viewer,
        _ => []
    };
}
