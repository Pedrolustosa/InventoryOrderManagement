using InventoryOrderManagement.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<GetAllProductsUseCase>();

        return services;
    }
}
