using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Api.IntegrationTests.Finance;

public class FinanceEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public FinanceEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayable_ThenPartialPayment_ReducesTheBalanceWithoutASecondMovement()
    {
        var accountResponse = await _client.PostAsJsonAsync("/api/finance/cash-accounts", new CreateCashAccountRequest
        {
            Name = "Operating cash",
            AccountType = CashAccountType.Cash,
            OpeningBalance = 1000m,
            OpeningBalanceDate = new DateOnly(2026, 11, 1)
        });
        accountResponse.EnsureSuccessStatusCode();
        var account = await accountResponse.Content.ReadFromJsonAsync<CashAccountResponse>(JsonOptions);

        var createdResponse = await _client.PostAsJsonAsync("/api/finance/payables", new CreatePayableRequest
        {
            Description = "Goods",
            CounterpartyName = "Neighborhood Hardware",
            SourceType = FinancialSourceType.Manual,
            IssueDate = new DateOnly(2026, 11, 1),
            TotalAmount = 1500m,
            Installments =
            [
                new FinancialInstallmentRequest { InstallmentNumber = 1, DueDate = new DateOnly(2026, 11, 15), Amount = 750m },
                new FinancialInstallmentRequest { InstallmentNumber = 2, DueDate = new DateOnly(2026, 12, 15), Amount = 750m }
            ]
        });
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<FinancialTitleResponse>(JsonOptions);
        Assert.Equal(FinancialTitleStatus.Pending, created!.Status);
        Assert.StartsWith("FT-", created.TitleNumber);

        var settlementResponse = await _client.PostAsJsonAsync(
            $"/api/finance/payables/{created.Id}/installments/{created.Installments.First().Id}/settlements",
            new SettleInstallmentRequest
            {
                CashAccountId = account!.Id,
                PaymentMethod = FinancialPaymentMethod.Pix,
                SettledPrincipalAmount = 400m,
                OccurredAtUtc = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc)
            });
        settlementResponse.EnsureSuccessStatusCode();
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<FinancialSettlementResponse>(JsonOptions);
        Assert.Equal(400m, settlement!.CashAmount);

        var balance = await _client.GetFromJsonAsync<CashAccountBalanceResponse>(
            $"/api/finance/cash-accounts/{account.Id}/balance",
            JsonOptions);
        Assert.Equal(600m, balance!.CurrentBalance);

        var movements = await _client.GetFromJsonAsync<PagedMovements>(
            "/api/finance/cash-movements",
            JsonOptions);
        Assert.Equal(1, movements!.TotalItems);
    }

    [Fact]
    public async Task GetPayables_WithoutPermission_ReturnsForbidden()
    {
        using var factory = new LimitedFinanceApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/finance/payables");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPayables_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new AnonymousFinanceApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/finance/payables");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class PagedMovements
    {
        public int TotalItems { get; set; }
    }
}

public sealed class LimitedFinanceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTestFinanceLimited";

    public LimitedFinanceAuthenticationHandler(
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
            new(ClaimTypes.Name, "integration-finance@localhost"),
            new(ClaimTypes.Role, SystemRoles.Viewer)
        };
        claims.AddRange(Permissions.All
            .Where(permission => permission != Permissions.Finance.View)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

public sealed class LimitedFinanceApiFactory : ProductsApiFactory
{
    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = LimitedFinanceAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = LimitedFinanceAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, LimitedFinanceAuthenticationHandler>(
            LimitedFinanceAuthenticationHandler.SchemeName,
            _ => { });
    }
}

public sealed class AnonymousFinanceApiFactory : ProductsApiFactory
{
    protected override bool UseTestAuthentication => false;
}
