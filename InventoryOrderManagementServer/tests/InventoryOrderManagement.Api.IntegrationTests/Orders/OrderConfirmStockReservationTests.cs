using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.IntegrationTests.Orders;

public class OrderConfirmStockReservationTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OrderConfirmStockReservationTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Confirm_WithStock_ReturnsOkAndReservesQuantity()
    {
        var (customerId, productId) = await SeedCustomerProductAndStockAsync("confirm-ok", stockQuantity: 5);
        var order = await CreateSalesOrderAsync(customerId, productId, quantity: 2);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmed = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);

        var balance = await GetBalanceAsync(productId);
        Assert.Equal(5, balance.QuantityOnHand);
        Assert.Equal(2, balance.ReservedQuantity);
        Assert.Equal(3, balance.AvailableQuantity);
    }

    [Fact]
    public async Task Confirm_WithoutStock_ReturnsConflictAndLeavesDraftUnchanged()
    {
        var (customerId, productId) = await SeedCustomerProductAndStockAsync("confirm-409", stockQuantity: 1);
        var order = await CreateSalesOrderAsync(customerId, productId, quantity: 3);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.Equal("Insufficient stock", problem!.Title);
        Assert.Contains("Available: 1", problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("productName"));
        Assert.True(problem.Extensions.ContainsKey("requestedQuantity"));
        Assert.True(problem.Extensions.ContainsKey("availableQuantity"));

        var reloaded = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Draft, reloaded!.Status);

        var balance = await GetBalanceAsync(productId);
        Assert.Equal(1, balance.QuantityOnHand);
        Assert.Equal(0, balance.ReservedQuantity);
        Assert.Equal(1, balance.AvailableQuantity);
    }

    [Fact]
    public async Task Confirm_ConcurrentLastUnit_OnlyOneSucceeds()
    {
        var (customerId, productId) = await SeedCustomerProductAndStockAsync("confirm-race", stockQuantity: 1);
        var firstOrder = await CreateSalesOrderAsync(customerId, productId, quantity: 1);
        var secondOrder = await CreateSalesOrderAsync(customerId, productId, quantity: 1);

        var firstTask = _client.PostAsync($"/api/orders/{firstOrder.Id}/confirm", null);
        var secondTask = _client.PostAsync($"/api/orders/{secondOrder.Id}/confirm", null);
        var responses = await Task.WhenAll(firstTask, secondTask);

        var statuses = new[]
        {
            responses[0].StatusCode,
            responses[1].StatusCode
        };

        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);

        var balance = await GetBalanceAsync(productId);
        Assert.Equal(1, balance.QuantityOnHand);
        Assert.Equal(1, balance.ReservedQuantity);
        Assert.Equal(0, balance.AvailableQuantity);

        var firstReloaded = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{firstOrder.Id}", JsonOptions);
        var secondReloaded = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{secondOrder.Id}", JsonOptions);
        var confirmedCount = new[] { firstReloaded!.Status, secondReloaded!.Status }
            .Count(status => status == OrderStatus.Confirmed);
        Assert.Equal(1, confirmedCount);
    }

    private async Task<(Guid CustomerId, Guid ProductId)> SeedCustomerProductAndStockAsync(
        string sku,
        int stockQuantity)
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = $"Customer {sku}"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        var supplierResponse = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = $"Supplier {sku}"
        });
        supplierResponse.EnsureSuccessStatusCode();
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);

        var productResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Sku = sku,
            Name = $"Product {sku}",
            CurrentPrice = 25m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        if (stockQuantity > 0)
        {
            var receiveResponse = await _client.PostAsJsonAsync("/api/inventory/entries", new ReceiveStockRequest
            {
                ProductId = product!.Id,
                SupplierId = supplier!.Id,
                Quantity = stockQuantity
            });
            receiveResponse.EnsureSuccessStatusCode();
        }

        return (customer!.Id, product!.Id);
    }

    private async Task<OrderResponse> CreateSalesOrderAsync(Guid customerId, Guid productId, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = customerId,
            ShippingAddress = new OrderShippingAddressRequest
            {
                PostalCode = "01310-100",
                Street = "Av Paulista",
                Number = "1000",
                City = "Sao Paulo",
                State = "SP",
                Country = "BR"
            },
            Items =
            [
                new OrderItemRequest { ProductId = productId, Quantity = quantity }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<OrderCreatedResponse>(JsonOptions);
        return (await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{created!.Id}", JsonOptions))!;
    }

    private async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId) =>
        (await _client.GetFromJsonAsync<InventoryBalanceResponse>($"/api/inventory/balances/{productId}", JsonOptions))!;
}
