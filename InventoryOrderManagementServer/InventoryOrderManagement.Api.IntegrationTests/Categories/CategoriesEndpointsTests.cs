using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryOrderManagement.Application.Categories.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Api.IntegrationTests.Products;

namespace InventoryOrderManagement.Api.IntegrationTests.Categories;

public class CategoriesEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public CategoriesEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = "Devices"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        Assert.NotNull(category);
        Assert.Equal("Electronics", category.Name);
        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesCategory()
    {
        var created = await CreateCategoryAsync("Old Name");

        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{created.Id}",
            new UpdateCategoryRequest { Name = "New Name", Description = "Updated" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await _client.GetFromJsonAsync<CategoryResponse>(
            $"/api/categories/{created.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.Equal("New Name", get.Name);
        Assert.Equal("Updated", get.Description);
    }

    [Fact]
    public async Task PatchDeactivate_WithActiveProducts_ReturnsConflict()
    {
        var category = await CreateCategoryAsync("Busy Category");

        var productResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}"[..12],
            Name = "Item",
            CurrentPrice = 10m,
            CategoryId = category.Id
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var deactivate = await _client.PatchAsync($"/api/categories/{category.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
    }

    [Fact]
    public async Task PatchDeactivate_WithoutActiveProducts_ReturnsNoContent()
    {
        var category = await CreateCategoryAsync("Empty Category");

        var deactivate = await _client.PatchAsync($"/api/categories/{category.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var get = await _client.GetFromJsonAsync<CategoryResponse>(
            $"/api/categories/{category.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.False(get.IsActive);
    }

    private async Task<CategoryResponse> CreateCategoryAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest
        {
            Name = name,
            Description = null
        });

        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        Assert.NotNull(category);
        return category;
    }
}
