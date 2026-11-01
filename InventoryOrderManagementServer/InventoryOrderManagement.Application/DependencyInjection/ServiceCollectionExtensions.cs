using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Application.Products;
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

        return services;
    }
}
