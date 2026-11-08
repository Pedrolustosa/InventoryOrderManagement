using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Api.IntegrationTests.Returns;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace InventoryOrderManagement.Api.IntegrationTests.Orders;

public class OrderTimelineEndpointsTests : IClassFixture<ProductsApiFactory>
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly ReturnFlowApi _flow;
    private readonly ITestOutputHelper _output;

    public OrderTimelineEndpointsTests(ProductsApiFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _flow = new ReturnFlowApi(_client);
        _output = output;
    }

    [Fact]
    public async Task Timeline_ForAFullFlow_ReturnsTheExpectedContractInOrder()
    {
        // Confirm → ship (shipment) → deliver; receivable of 4 × 50.00 = 200.00.
        var sale = await _flow.SeedDeliveredSaleAsync("timeline-flow", quantity: 4, throughShipment: true);
        var receivable = Assert.Single((await _flow.GetTitlesAsync("receivables", sale.Order.Id)).Items);
        await SettleAsync(receivable.Id, 80m);

        // Customer return of 1 unit (50.00), restocked; it abates the open receivable.
        var created = await _flow.CreateCustomerReturnAsync(sale.Order, quantity: 1, ReturnFinancialTreatment.CustomerRefund);
        await _flow.ReceiveAndStartInspectionAsync(created.Id, created.Items.Single().Id, 1);
        (await _flow.CompleteInspectionAsync(created.Id, created.Items.Single().Id, restocked: 1, quarantined: 0))
            .EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/orders/{sale.Order.Id}/timeline");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine(raw);
        var timeline = JsonSerializer.Deserialize<OrderTimelineDto>(raw, JsonOptions)!;

        Assert.Equal(sale.Order.Id, timeline.OrderId);
        Assert.Equal(sale.Order.OrderNumber, timeline.OrderNumber);
        Assert.Equal(OrderStatus.Delivered, timeline.CurrentStatus);

        var codes = timeline.Entries.Select(entry => entry.EventType).ToList();
        foreach (var expected in new[]
                 {
                     "order.created", "order.confirmed", "stock.reserved", "shipment.created", "shipment.dispatched",
                     "stock.issued", "finance.receivable_created", "order.shipped", "shipment.in_transit",
                     "shipment.delivered", "order.delivered", "finance.settled", "return.requested",
                     "return.received", "return.completed", "stock.return_restocked", "finance.abated"
                 })
        {
            Assert.Contains(expected, codes);
        }

        // Chronological, and the business flow is respected.
        Assert.Equal(timeline.Entries.OrderBy(entry => entry.OccurredAtUtc).Select(entry => entry.EventType), codes);
        Assert.True(codes.IndexOf("order.confirmed") < codes.IndexOf("shipment.dispatched"));
        Assert.True(codes.IndexOf("shipment.dispatched") < codes.IndexOf("order.delivered"));
        Assert.True(codes.IndexOf("order.delivered") < codes.IndexOf("return.completed"));
        Assert.True(codes.IndexOf("return.completed") < codes.IndexOf("finance.abated"));

        var dispatched = timeline.Entries.Single(entry => entry.EventType == "shipment.dispatched");
        Assert.Equal(OrderTimelineModule.Shipment, dispatched.Module);
        Assert.Equal("Shipment", dispatched.ReferenceType);
        Assert.Equal("Shipped", dispatched.Status);
        Assert.Equal("Acme", dispatched.Summary.CarrierName);

        var issued = timeline.Entries.Single(entry => entry.EventType == "stock.issued");
        Assert.Equal("Product", issued.ReferenceType);
        Assert.Equal(sale.ProductId, issued.ReferenceId);
        Assert.Equal(4, issued.Summary.Quantity);

        var settled = timeline.Entries.Single(entry => entry.EventType == "finance.settled");
        Assert.Equal("FinancialTitle", settled.ReferenceType);
        Assert.Equal(receivable.Id, settled.ReferenceId);
        Assert.Equal("PartiallyPaid", settled.Status);
        Assert.Equal(80m, settled.Summary.Amount);
        Assert.Equal(TestAuthenticationHandler.UserId, settled.PerformedBy?.UserId);

        var abated = timeline.Entries.Single(entry => entry.EventType == "finance.abated");
        Assert.Equal(50m, abated.Summary.Amount);
        Assert.Equal(FinancialPaymentMethod.ReturnCredit, abated.Summary.PaymentMethod);

        var requested = timeline.Entries.Single(entry => entry.EventType == "return.requested");
        Assert.Equal("ReturnRequest", requested.ReferenceType);
        Assert.Equal(created.Id, requested.ReferenceId);
        Assert.Equal(created.ReturnNumber, requested.Summary.ReturnNumber);

        // Timestamps are explicit UTC ("Z").
        Assert.All(timeline.Entries, entry => Assert.Equal(DateTimeKind.Utc, entry.OccurredAtUtc.Kind));

        // Codes only: no translated text anywhere in the payload.
        Assert.DoesNotContain("Pedido", raw);
        Assert.DoesNotContain("\"title\"", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Timeline_ForAnUnknownOrder_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.Equal(404, problem!.Status);
    }

    private async Task SettleAsync(Guid receivableId, decimal amount)
    {
        var accountResponse = await _client.PostAsJsonAsync("/api/finance/cash-accounts", new CreateCashAccountRequest
        {
            Name = $"Timeline cash {Guid.NewGuid():N}",
            AccountType = CashAccountType.Cash,
            OpeningBalance = 0m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        accountResponse.EnsureSuccessStatusCode();
        var account = (await accountResponse.Content.ReadFromJsonAsync<CashAccountResponse>(JsonOptions))!;
        var title = (await _client.GetFromJsonAsync<FinancialTitleResponse>($"/api/finance/receivables/{receivableId}", JsonOptions))!;

        (await _client.PostAsJsonAsync(
            $"/api/finance/receivables/{receivableId}/installments/{title.Installments.Single().Id}/settlements",
            new SettleInstallmentRequest
            {
                CashAccountId = account.Id,
                PaymentMethod = FinancialPaymentMethod.Pix,
                SettledPrincipalAmount = amount,
                OccurredAtUtc = DateTime.UtcNow
            })).EnsureSuccessStatusCode();
    }
}

public class OrderTimelineWithoutOrdersViewTests : IClassFixture<WithoutOrdersViewApiFactory>
{
    private readonly HttpClient _client;

    public OrderTimelineWithoutOrdersViewTests(WithoutOrdersViewApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Timeline_WithoutOrdersView_Returns403()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class OrderTimelineWithoutFinanceViewTests : IClassFixture<WithoutFinanceViewApiFactory>
{
    private readonly HttpClient _client;
    private readonly ReturnFlowApi _flow;

    public OrderTimelineWithoutFinanceViewTests(WithoutFinanceViewApiFactory factory)
    {
        _client = factory.CreateClient();
        _flow = new ReturnFlowApi(_client);
    }

    [Fact]
    public async Task Timeline_WithoutFinanceView_OmitsFinanceEntries()
    {
        // Dispatch creates a receivable, so finance data exists for this order.
        var sale = await _flow.SeedDeliveredSaleAsync("timeline-nofin", quantity: 2, throughShipment: true);

        var timeline = await _client.GetFromJsonAsync<OrderTimelineDto>(
            $"/api/orders/{sale.Order.Id}/timeline",
            OrderTimelineEndpointsTests.JsonOptions);

        Assert.DoesNotContain(timeline!.Entries, entry => entry.Module == OrderTimelineModule.Finance);
        Assert.Contains(timeline.Entries, entry => entry.EventType == "stock.issued");
        Assert.Contains(timeline.Entries, entry => entry.EventType == "shipment.dispatched");
        Assert.Contains(timeline.Entries, entry => entry.EventType == "order.delivered");
    }
}

public sealed class WithoutOrdersViewApiFactory : RestrictedPermissionsApiFactory
{
    protected override string ExcludedPermission => Permissions.Orders.View;
}

/// <summary>Has every permission except Finance.View (finance write permissions do not imply viewing).</summary>
public sealed class WithoutFinanceViewApiFactory : RestrictedPermissionsApiFactory
{
    protected override string ExcludedPermission => Permissions.Finance.View;
}

/// <summary>Authenticates a user holding every permission except one.</summary>
public abstract class RestrictedPermissionsApiFactory : ProductsApiFactory
{
    protected abstract string ExcludedPermission { get; }

    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.AddSingleton(new ExcludedTestPermission(ExcludedPermission));
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = RestrictedPermissionsAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = RestrictedPermissionsAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, RestrictedPermissionsAuthenticationHandler>(
            RestrictedPermissionsAuthenticationHandler.SchemeName,
            _ => { });
    }
}

public sealed record ExcludedTestPermission(string Permission);

public sealed class RestrictedPermissionsAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTestRestricted";

    private readonly ExcludedTestPermission _excluded;

    public RestrictedPermissionsAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ExcludedTestPermission excluded)
        : base(options, logger, encoder)
    {
        _excluded = excluded;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestAuthenticationHandler.UserId.ToString()),
            new(ClaimTypes.Name, "integration-restricted@localhost"),
            new(ClaimTypes.Role, SystemRoles.Viewer)
        };
        claims.AddRange(Permissions.All
            .Where(permission => permission != _excluded.Permission)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
