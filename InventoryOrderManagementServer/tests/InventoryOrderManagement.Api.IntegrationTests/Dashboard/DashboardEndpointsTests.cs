using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryOrderManagement.Api.IntegrationTests.Dashboard;

public class DashboardEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ProductsApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(ProductsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_Authenticated_ReturnsOkWithSections()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<OperationalDashboardSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.NotEqual(default, summary.GeneratedAtUtc);
        Assert.True(summary.Products.Total >= 0);
        Assert.True(summary.Customers.Total >= 0);
        Assert.True(summary.Suppliers.Total >= 0);
        Assert.True(summary.Orders.Total >= 0);
        Assert.True(summary.Purchases.Total >= 0);
        Assert.NotNull(summary.Finance);
        Assert.NotNull(summary.Shipping);
        Assert.NotNull(summary.Returns);
        Assert.NotNull(summary.Notifications);
    }

    [Fact]
    public async Task GetTrends_WithGroupByMonth_ReturnsOk()
    {
        var response = await _client.GetAsync(
            "/api/dashboard/trends?period=CurrentMonth&groupBy=Month");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trends = await response.Content.ReadFromJsonAsync<DashboardTrendsResponse>(JsonOptions);

        Assert.NotNull(trends);
        Assert.Equal("Month", trends.GroupBy);
        Assert.NotNull(trends.Items);
    }

    [Fact]
    public async Task GetRecentOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/dashboard/recent-orders?take=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DashboardRecentOrdersResponse>(JsonOptions);

        Assert.NotNull(payload);
        Assert.NotNull(payload.Items);
        Assert.True(payload.Items.Count <= 5);
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var anonymousFactory = new AnonymousDashboardApiFactory();
        var client = anonymousFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class AnonymousDashboardApiFactory : ProductsApiFactory
    {
        protected override bool UseTestAuthentication => false;
    }
}
