using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryOrderManagement.Api.IntegrationTests.Products;

public class ProductsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"iom-api-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<InventoryDbContext>));
            services.RemoveAll(typeof(InventoryDbContext));

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));
        });

        
                                        builder.ConfigureTestServices(services =>
        {
            if (UseTestAuthentication)
            {
                ConfigureTestAuthentication(services);
            }

            services.PostConfigure<MvcOptions>(options =>
            {
                for (var index = options.Filters.Count - 1; index >= 0; index--)
                {
                    if (options.Filters[index] is ServiceFilterAttribute serviceFilter
                        && serviceFilter.ServiceType == typeof(InventoryOrderManagement.Api.Security.AntiforgeryFilter))
                    {
                        options.Filters.RemoveAt(index);
                    }
                }
            });
        });
    }

    protected virtual bool UseTestAuthentication => true;

    protected virtual void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
            TestAuthenticationHandler.SchemeName,
            _ => { });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        TryDelete(_databasePath);
        TryDelete(_databasePath + "-shm");
        TryDelete(_databasePath + "-wal");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            
        }
    }
}
