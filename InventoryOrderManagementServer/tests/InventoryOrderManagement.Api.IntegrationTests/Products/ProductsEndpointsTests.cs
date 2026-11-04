using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;

namespace InventoryOrderManagement.Api.IntegrationTests.Products;

public class ProductsEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid CategoryId = CategoryConfiguration.UncategorizedCategoryId;

    private readonly HttpClient _client;

    public ProductsEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_ReturnsCreated()
    {
        var payload = new CreateProductRequest
        {
            Sku = "kb-001",
            Name = "Mechanical Keyboard",
            Description = "RGB",
            CurrentPrice = 249.90m,
            CategoryId = CategoryId
        };

        var response = await _client.PostAsJsonAsync("/api/products", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.NotNull(product);
        Assert.Equal("KB-001", product.Sku);
        Assert.Equal(CategoryId, product.CategoryId);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Get_AfterCreate_ReturnsProducts()
    {
        await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Sku = "ms-100",
            Name = "Mouse",
            CurrentPrice = 99.9m,
            CategoryId = CategoryId
        });

        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(JsonOptions);

        Assert.NotNull(products);
        Assert.Contains(products, item => item.Sku == "MS-100");
    }

    [Fact]
    public async Task Post_WithInvalidPayload_ReturnsBadRequestProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Sku = "",
            Name = "Invalid",
            CurrentPrice = 10m,
            CategoryId = CategoryId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid request data", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Post_WithDuplicateSku_ReturnsConflictProblemDetails()
    {
        var payload = new CreateProductRequest
        {
            Sku = "DUP-1",
            Name = "First",
            CurrentPrice = 10m,
            CategoryId = CategoryId
        };

        var first = await _client.PostAsJsonAsync("/api/products", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/products", payload);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("Duplicate SKU", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(409, document.RootElement.GetProperty("status").GetInt32());
    }
}
