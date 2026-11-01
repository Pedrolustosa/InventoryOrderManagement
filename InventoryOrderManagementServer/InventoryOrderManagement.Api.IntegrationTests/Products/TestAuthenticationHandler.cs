using System.Security.Claims;
using System.Text.Encodings.Web;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Api.IntegrationTests.Products;

/// <summary>
/// Authenticates every request as an administrator so the existing API tests keep exercising business rules.
/// </summary>
public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTest";

    public static readonly Guid UserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public TestAuthenticationHandler(
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
            new(ClaimTypes.NameIdentifier, UserId.ToString()),
            new(ClaimTypes.Name, "integration-admin@localhost"),
            new(ClaimTypes.Role, SystemRoles.Administrator)
        };

        claims.AddRange(Permissions.All.Select(permission => new Claim(Permissions.ClaimType, permission)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
