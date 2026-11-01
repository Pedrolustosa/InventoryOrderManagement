using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Api.IntegrationTests.Customers;

public class CustomersEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public CustomersEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "John Smith",
                TaxIdentifier = "123.456.789-00",
                Email = "john@example.com",
                Phone = "+55 81 99999-8888",
                Address = new CustomerAddressDto
                {
                    PostalCode = "50000-000",
                    Street = "Example Street",
                    Number = "100",
                    Complement = "Apartment 301",
                    Neighborhood = "Boa Viagem",
                    City = "Recife",
                    State = "PE",
                    Country = "Brazil"
                }
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);
        Assert.Equal("John Smith", customer.Name);
        Assert.Equal(CustomerType.Individual, customer.CustomerType);
        Assert.NotNull(customer.Address);
        Assert.Equal("Recife", customer.Address.City);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public async Task Get_WithFilters_ReturnsMatchingCustomers()
    {
        await CreateCustomerAsync("Alpha Filters Co", CustomerType.Company, "44.444.444/0001-44");
        await CreateCustomerAsync("Beta Other", CustomerType.Individual, null);

        var response = await _client.GetAsync(
            "/api/customers?isActive=true&customerType=Company&name=Filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customers = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>(JsonOptions);
        Assert.NotNull(customers);
        Assert.Contains(customers, item => item.Name == "Alpha Filters Co");
        Assert.DoesNotContain(customers, item => item.Name == "Beta Other");
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesCustomer()
    {
        var created = await CreateCustomerAsync("Old Customer", CustomerType.Individual, "33.333.333-33");

        var response = await _client.PutAsJsonAsync(
            $"/api/customers/{created.Id}",
            new UpdateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "New Customer",
                TaxIdentifier = "33.333.333-33",
                Email = "updated@example.com"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await _client.GetFromJsonAsync<CustomerResponse>(
            $"/api/customers/{created.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.Equal("New Customer", get.Name);
        Assert.Equal("updated@example.com", get.Email);
    }

    [Fact]
    public async Task Post_WithDuplicateTaxIdentifier_ReturnsConflict()
    {
        await CreateCustomerAsync("First", CustomerType.Individual, "98.765.432-10");

        var response = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = "Second",
            TaxIdentifier = "9876543210"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Duplicate tax identifier", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchDeactivate_ReturnsNoContent()
    {
        var created = await CreateCustomerAsync("To Deactivate", CustomerType.Company, null);

        var deactivate = await _client.PatchAsync($"/api/customers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var get = await _client.GetFromJsonAsync<CustomerResponse>(
            $"/api/customers/{created.Id}",
            JsonOptions);

        Assert.NotNull(get);
        Assert.False(get.IsActive);
    }

    [Fact]
    public async Task Put_WhenInactive_ReturnsBadRequest()
    {
        var created = await CreateCustomerAsync("Inactive Target", CustomerType.Individual, null);
        var deactivate = await _client.PatchAsync($"/api/customers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var response = await _client.PutAsJsonAsync(
            $"/api/customers/{created.Id}",
            new UpdateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "Still Inactive"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<CustomerResponse> CreateCustomerAsync(
        string name,
        CustomerType customerType,
        string? taxIdentifier)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest
            {
                CustomerType = customerType,
                Name = name,
                TaxIdentifier = taxIdentifier
            },
            JsonOptions);

        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);
        return customer;
    }
}
