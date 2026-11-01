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

public sealed class ProductsApiFactory : WebApplicationFactory<Program>
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

        // Future identity coverage, not part of this suite: an invalid password is
        // indistinguishable from an unknown email, an inactive account cannot sign in,
        // the fifth failure locks the account, Viewer cannot write, InventoryOperator
        // and SalesOperator stay inside their modules, a security stamp ends the session,
        // and the audit log stores no secret.
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });

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
            // Temp SQLite files may remain locked briefly on Windows; safe to ignore in tests.
        }
    }
}
