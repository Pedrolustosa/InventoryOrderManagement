using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Application.Products;
using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Suppliers;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<GetAllProductsUseCase>();
        services.AddScoped<CreateCategoryUseCase>();
        services.AddScoped<GetAllCategoriesUseCase>();
        services.AddScoped<GetCategoryByIdUseCase>();
        services.AddScoped<UpdateCategoryUseCase>();
        services.AddScoped<DeactivateCategoryUseCase>();
        services.AddScoped<CreateSupplierUseCase>();
        services.AddScoped<GetAllSuppliersUseCase>();
        services.AddScoped<GetSupplierByIdUseCase>();
        services.AddScoped<UpdateSupplierUseCase>();
        services.AddScoped<DeactivateSupplierUseCase>();
        services.AddScoped<CreateCustomerUseCase>();
        services.AddScoped<GetAllCustomersUseCase>();
        services.AddScoped<GetCustomerByIdUseCase>();
        services.AddScoped<UpdateCustomerUseCase>();
        services.AddScoped<DeactivateCustomerUseCase>();
        services.AddScoped<ReceiveStockUseCase>();
        services.AddScoped<RegisterStockExitUseCase>();
        services.AddScoped<AdjustStockUseCase>();
        services.AddScoped<UpdateMinimumStockLevelUseCase>();
        services.AddScoped<GetInventoryBalanceByProductUseCase>();
        services.AddScoped<GetAllInventoryBalancesUseCase>();
        services.AddScoped<GetLowStockProductsUseCase>();
        services.AddScoped<GetStockMovementsUseCase>();
        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<UpdateDraftOrderUseCase>();
        services.AddScoped<GetOrderByIdUseCase>();
        services.AddScoped<GetAllOrdersUseCase>();
        services.AddScoped<ConfirmOrderUseCase>();
        services.AddScoped<ShipOrderUseCase>();
        services.AddScoped<DeliverOrderUseCase>();
        services.AddScoped<CancelOrderUseCase>();
        services.AddScoped<GetDashboardSummaryUseCase>();
        services.AddScoped<GetInventoryReportUseCase>();
        services.AddScoped<GetLowStockReportUseCase>();
        services.AddScoped<GetStockMovementsReportUseCase>();
        services.AddScoped<GetOrderSummaryReportUseCase>();
        services.AddScoped<GetOrderTrendReportUseCase>();
        services.AddScoped<GetBestSellingProductsReportUseCase>();
        services.AddScoped<GetSalesByCustomerReportUseCase>();
        services.AddScoped<GetCancellationReportUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();
        services.AddScoped<ChangePasswordUseCase>();
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<GetAllUsersUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<UpdateUserUseCase>();
        services.AddScoped<DeactivateUserUseCase>();
        services.AddScoped<AssignUserRolesUseCase>();
        services.AddScoped<CreateRoleUseCase>();
        services.AddScoped<GetAllRolesUseCase>();
        services.AddScoped<GetRoleByIdUseCase>();
        services.AddScoped<UpdateRoleUseCase>();
        services.AddScoped<DeleteRoleUseCase>();
        services.AddScoped<GetAllPermissionsUseCase>();
        services.AddScoped<GetRolePermissionsUseCase>();
        services.AddScoped<UpdateRolePermissionsUseCase>();
        services.AddScoped<GetSecurityAuditLogsUseCase>();
        services.AddScoped<CreatePurchaseOrderUseCase>();
        services.AddScoped<GetAllPurchaseOrdersUseCase>();
        services.AddScoped<GetPurchaseOrderByIdUseCase>();
        services.AddScoped<UpdatePurchaseOrderUseCase>();
        services.AddScoped<SendPurchaseOrderUseCase>();
        services.AddScoped<ReceivePurchaseOrderUseCase>();
        services.AddScoped<GetPurchaseReceiptsUseCase>();
        services.AddScoped<GetPurchaseReceiptByIdUseCase>();
        services.AddScoped<CancelPurchaseOrderUseCase>();

        return services;
    }
}
