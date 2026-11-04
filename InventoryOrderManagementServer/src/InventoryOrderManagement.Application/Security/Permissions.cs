namespace InventoryOrderManagement.Application.Security;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string MustChangePasswordClaimType = "must_change_password";

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Deactivate = "Users.Deactivate";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
        public const string ManagePermissions = "Roles.ManagePermissions";
    }

    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Deactivate = "Products.Deactivate";
    }

    public static class Categories
    {
        public const string View = "Categories.View";
        public const string Create = "Categories.Create";
        public const string Update = "Categories.Update";
        public const string Deactivate = "Categories.Deactivate";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Create = "Suppliers.Create";
        public const string Update = "Suppliers.Update";
        public const string Deactivate = "Suppliers.Deactivate";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Create = "Customers.Create";
        public const string Update = "Customers.Update";
        public const string Deactivate = "Customers.Deactivate";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Receive = "Inventory.Receive";
        public const string Exit = "Inventory.Exit";
        public const string Adjust = "Inventory.Adjust";
        public const string MinimumStockUpdate = "Inventory.MinimumStock.Update";
    }

    public static class Orders
    {
        public const string View = "Orders.View";
        public const string Create = "Orders.Create";
        public const string UpdateDraft = "Orders.UpdateDraft";
        public const string Confirm = "Orders.Confirm";
        public const string Ship = "Orders.Ship";
        public const string Deliver = "Orders.Deliver";
        public const string Cancel = "Orders.Cancel";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
    }

    public static class Purchases
    {
        public const string View = "Purchases.View";
        public const string Create = "Purchases.Create";
        public const string UpdateDraft = "Purchases.UpdateDraft";
        public const string Send = "Purchases.Send";
        public const string Receive = "Purchases.Receive";
        public const string Cancel = "Purchases.Cancel";
    }

    public static class Security
    {
        public const string AuditView = "Security.Audit.View";
    }

    public static class Finance
    {
        public const string View = "Finance.View";
        public const string PayablesCreate = "Finance.Payables.Create";
        public const string PayablesUpdate = "Finance.Payables.Update";
        public const string PayablesCancel = "Finance.Payables.Cancel";
        public const string PayablesSettle = "Finance.Payables.Settle";
        public const string ReceivablesCreate = "Finance.Receivables.Create";
        public const string ReceivablesUpdate = "Finance.Receivables.Update";
        public const string ReceivablesCancel = "Finance.Receivables.Cancel";
        public const string ReceivablesSettle = "Finance.Receivables.Settle";
        public const string CashAccountsView = "Finance.CashAccounts.View";
        public const string CashAccountsManage = "Finance.CashAccounts.Manage";
        public const string CashFlowView = "Finance.CashFlow.View";
        public const string ReportsView = "Finance.Reports.View";
    }

    public static class Returns
    {
        public const string View = "Returns.View";
        public const string CustomerCreate = "Returns.Customer.Create";
        public const string CustomerReceive = "Returns.Customer.Receive";
        public const string CustomerInspect = "Returns.Customer.Inspect";
        public const string SupplierCreate = "Returns.Supplier.Create";
        public const string SupplierAuthorize = "Returns.Supplier.Authorize";
        public const string SupplierDispatch = "Returns.Supplier.Dispatch";
        public const string Cancel = "Returns.Cancel";
    }

    public static class Shipments
    {
        public const string View = "Shipments.View";
        public const string Create = "Shipments.Create";
        public const string Update = "Shipments.Update";
        public const string Dispatch = "Shipments.Dispatch";
        public const string Advance = "Shipments.Advance";
    }

    public static class Notifications
    {
        public const string View = "Notifications.View";
        public const string MarkRead = "Notifications.MarkRead";
    }

    public static readonly IReadOnlyList<string> All =
    [
        Users.View,
        Users.Create,
        Users.Update,
        Users.Deactivate,
        Roles.View,
        Roles.Create,
        Roles.Update,
        Roles.Delete,
        Roles.ManagePermissions,
        Products.View,
        Products.Create,
        Products.Update,
        Products.Deactivate,
        Categories.View,
        Categories.Create,
        Categories.Update,
        Categories.Deactivate,
        Suppliers.View,
        Suppliers.Create,
        Suppliers.Update,
        Suppliers.Deactivate,
        Customers.View,
        Customers.Create,
        Customers.Update,
        Customers.Deactivate,
        Inventory.View,
        Inventory.Receive,
        Inventory.Exit,
        Inventory.Adjust,
        Inventory.MinimumStockUpdate,
        Orders.View,
        Orders.Create,
        Orders.UpdateDraft,
        Orders.Confirm,
        Orders.Ship,
        Orders.Deliver,
        Orders.Cancel,
        Reports.View,
        Purchases.View,
        Purchases.Create,
        Purchases.UpdateDraft,
        Purchases.Send,
        Purchases.Receive,
        Purchases.Cancel,
        Security.AuditView,
        Finance.View,
        Finance.PayablesCreate,
        Finance.PayablesUpdate,
        Finance.PayablesCancel,
        Finance.PayablesSettle,
        Finance.ReceivablesCreate,
        Finance.ReceivablesUpdate,
        Finance.ReceivablesCancel,
        Finance.ReceivablesSettle,
        Finance.CashAccountsView,
        Finance.CashAccountsManage,
        Finance.CashFlowView,
        Finance.ReportsView,
        Returns.View,
        Returns.CustomerCreate,
        Returns.CustomerReceive,
        Returns.CustomerInspect,
        Returns.SupplierCreate,
        Returns.SupplierAuthorize,
        Returns.SupplierDispatch,
        Returns.Cancel,
        Shipments.View,
        Shipments.Create,
        Shipments.Update,
        Shipments.Dispatch,
        Shipments.Advance,
        Notifications.View,
        Notifications.MarkRead
    ];

    public static bool IsKnown(string permission) => All.Contains(permission);
}

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string InventoryOperator = "InventoryOperator";
    public const string SalesOperator = "SalesOperator";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        Manager,
        InventoryOperator,
        SalesOperator,
        Viewer
    ];
}

public static class SecurityActions
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string AccountLocked = "AccountLocked";
    public const string Logout = "Logout";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserDeactivated = "UserDeactivated";
    public const string RoleCreated = "RoleCreated";
    public const string RoleUpdated = "RoleUpdated";
    public const string RoleDeleted = "RoleDeleted";
    public const string PermissionsUpdated = "PermissionsUpdated";
    public const string RolesAssigned = "RolesAssigned";
    public const string PasswordChanged = "PasswordChanged";
    public const string PurchaseOrderCreated = "PurchaseOrderCreated";
    public const string PurchaseOrderUpdated = "PurchaseOrderUpdated";
    public const string PurchaseOrderSent = "PurchaseOrderSent";
    public const string PurchaseReceiptCreated = "PurchaseReceiptCreated";
    public const string PurchaseOrderCancelled = "PurchaseOrderCancelled";
    public const string FinancialTitleCreated = "FinancialTitleCreated";
    public const string FinancialTitleUpdated = "FinancialTitleUpdated";
    public const string FinancialTitleCancelled = "FinancialTitleCancelled";
    public const string FinancialSettlementCreated = "FinancialSettlementCreated";
    public const string CashAccountCreated = "CashAccountCreated";
    public const string CashAccountUpdated = "CashAccountUpdated";
    public const string CashAccountDeactivated = "CashAccountDeactivated";
    public const string CustomerReturnCreated = "CustomerReturnCreated";
    public const string SupplierReturnCreated = "SupplierReturnCreated";
    public const string ReturnUpdated = "ReturnUpdated";
    public const string CustomerReturnReceived = "CustomerReturnReceived";
    public const string ReturnInspectionStarted = "ReturnInspectionStarted";
    public const string ReturnInspectionCompleted = "ReturnInspectionCompleted";
    public const string SupplierReturnAuthorized = "SupplierReturnAuthorized";
    public const string SupplierReturnDispatched = "SupplierReturnDispatched";
    public const string ReturnCancelled = "ReturnCancelled";
    public const string ShipmentCreated = "ShipmentCreated";
    public const string ShipmentUpdated = "ShipmentUpdated";
    public const string ShipmentDispatched = "ShipmentDispatched";
    public const string ShipmentInTransit = "ShipmentInTransit";
    public const string ShipmentDelivered = "ShipmentDelivered";
    public const string ShipmentFailed = "ShipmentFailed";
    public const string ShipmentResumed = "ShipmentResumed";
}

public static class SecurityEntities
{
    public const string User = "User";
    public const string Role = "Role";
    public const string Session = "Session";
    public const string ReturnRequest = "ReturnRequest";
    public const string Shipment = "Shipment";
}

public static class SecurityOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
}
