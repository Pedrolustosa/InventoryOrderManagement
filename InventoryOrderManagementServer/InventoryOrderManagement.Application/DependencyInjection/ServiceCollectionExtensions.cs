using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Products;
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

        return services;
    }
}
