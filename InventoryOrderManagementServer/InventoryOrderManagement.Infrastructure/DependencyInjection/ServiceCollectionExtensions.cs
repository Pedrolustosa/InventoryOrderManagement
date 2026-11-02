using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Infrastructure.Identity;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Queries;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
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
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseReceiptRepository, PurchaseReceiptRepository>();
        services.AddScoped<IFinancialTitleRepository, FinancialTitleRepository>();
        services.AddScoped<IFinancialSettlementRepository, FinancialSettlementRepository>();
        services.AddScoped<ICashAccountRepository, CashAccountRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<IInventoryReportsQuery, InventoryReportsQuery>();
        services.AddScoped<IStockMovementsReportsQuery, StockMovementsReportsQuery>();
        services.AddScoped<IOrderReportsQuery, OrderReportsQuery>();
        services.AddScoped<ISalesReportsQuery, SalesReportsQuery>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // RequireConfirmedAccount stays false: there is no e-mail delivery, so confirmation
        // and the forgot/reset password endpoints are not exposed. Identity stores remain ready for MFA.
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<InventoryDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        services.AddHttpContextAccessor();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ".Iom.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events ??= new CookieAuthenticationEvents();
            options.Events.OnRedirectToLogin = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication is required.",
                    Instance = context.Request.Path
                });
            };
            options.Events.OnRedirectToAccessDenied = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to perform this action.",
                    Instance = context.Request.Path
                });
            };

            var validatePrincipal = options.Events.OnValidatePrincipal;
            options.Events.OnValidatePrincipal = async context =>
            {
                if (validatePrincipal is not null)
                {
                    await validatePrincipal(context);
                }

                if (context.Principal?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.Principal);
                if (user is null || !user.IsActive)
                {
                    context.RejectPrincipal();
                }
            };
        });

        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleAccountService, RoleAccountService>();
        services.AddScoped<ISecurityAuditQuery, SecurityAuditQueryService>();

        return services;
    }
}
