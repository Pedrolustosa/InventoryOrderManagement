using InventoryOrderManagement.Application.Categories;
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

        return services;
    }
}
