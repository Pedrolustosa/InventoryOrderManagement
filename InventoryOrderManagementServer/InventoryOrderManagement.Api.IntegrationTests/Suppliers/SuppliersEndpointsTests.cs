using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Suppliers.Dtos;

namespace InventoryOrderManagement.Api.IntegrationTests.Suppliers;

public class SuppliersEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public SuppliersEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = "Tech Components Ltd.",
            TaxIdentifier = "12.345.678/0001-90",
            ContactName = "John Smith",
            Email = "contact@techcomponents.com",
            Phone = "+55 81 3333-4444",
            Notes = "Primary supplier"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        Assert.Equal("Tech Components Ltd.", supplier.Name);
        Assert.Equal("12.345.678/0001-90", supplier.TaxIdentifier);
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public async Task Get_WithFilters_ReturnsMatchingSuppliers()
    {
        await CreateSupplierAsync("Alpha Filters Co", taxIdentifier: null);
        await CreateSupplierAsync("Beta Other", taxIdentifier: null);

        var response = await _client.GetAsync("/api/suppliers?isActive=true&name=Filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var suppliers = await response.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(suppliers);
        Assert.Contains(suppliers, item => item.Name == "Alpha Filters Co");
        Assert.DoesNotContain(suppliers, item => item.Name == "Beta Other");
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesSupplier()
    {
        var created = await CreateSupplierAsync("Old Supplier", "11.111.111/0001-11");

        var response = await _client.PutAsJsonAsync(
            $"/api/suppliers/{created.Id}",
            new UpdateSupplierRequest
            {
                Name = "New Supplier",
                TaxIdentifier = "11.111.111/0001-11",
                Email = "support@example.com"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await _client.GetFromJsonAsync<SupplierResponse>(
            $"/api/suppliers/{created.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.Equal("New Supplier", get.Name);
        Assert.Equal("support@example.com", get.Email);
    }

    [Fact]
    public async Task Post_WithDuplicateTaxIdentifier_ReturnsConflict()
    {
        await CreateSupplierAsync("First", "98.765.432/0001-10");

        var response = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = "Second",
            TaxIdentifier = "98765432000110"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Duplicate tax identifier", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchDeactivate_ReturnsNoContent()
    {
        var created = await CreateSupplierAsync("To Deactivate", null);

        var deactivate = await _client.PatchAsync($"/api/suppliers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var get = await _client.GetFromJsonAsync<SupplierResponse>(
            $"/api/suppliers/{created.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.False(get.IsActive);
    }

    [Fact]
    public async Task Put_WhenInactive_ReturnsBadRequest()
    {
        var created = await CreateSupplierAsync("Inactive Target", null);
        var deactivate = await _client.PatchAsync($"/api/suppliers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var response = await _client.PutAsJsonAsync(
            $"/api/suppliers/{created.Id}",
            new UpdateSupplierRequest { Name = "Still Inactive" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<SupplierResponse> CreateSupplierAsync(string name, string? taxIdentifier)
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = name,
            TaxIdentifier = taxIdentifier
        });

        response.EnsureSuccessStatusCode();
        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        return supplier;
    }
}
