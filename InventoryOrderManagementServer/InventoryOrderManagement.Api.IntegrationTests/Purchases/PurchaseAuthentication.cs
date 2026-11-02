using System.Security.Claims;
using System.Text.Encodings.Web;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Api.IntegrationTests.Products;

/// <summary>
/// Administrator principal without Purchases.Receive, used to prove the receive policy returns 403.
/// </summary>
public sealed class LimitedPurchaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTestLimited";

    public LimitedPurchaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestAuthenticationHandler.UserId.ToString()),
            new(ClaimTypes.Name, "integration-limited@localhost"),
            new(ClaimTypes.Role, SystemRoles.Administrator)
        };

        claims.AddRange(Permissions.All
            .Where(permission => permission != Permissions.Purchases.Receive)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class LimitedPurchaseApiFactory : ProductsApiFactory
{
    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = LimitedPurchaseAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = LimitedPurchaseAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, LimitedPurchaseAuthenticationHandler>(
            LimitedPurchaseAuthenticationHandler.SchemeName,
            _ => { });
    }
}

public sealed class AnonymousPurchaseApiFactory : ProductsApiFactory
{
    protected override bool UseTestAuthentication => false;
}
