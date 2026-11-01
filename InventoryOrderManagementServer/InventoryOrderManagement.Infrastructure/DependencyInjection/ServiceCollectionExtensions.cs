using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Queries;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryReportsQuery, InventoryReportsQuery>();
        services.AddScoped<IStockMovementsReportsQuery, StockMovementsReportsQuery>();
        services.AddScoped<IOrderReportsQuery, OrderReportsQuery>();
        services.AddScoped<ISalesReportsQuery, SalesReportsQuery>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
