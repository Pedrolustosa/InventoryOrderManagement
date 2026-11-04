using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;

namespace InventoryOrderManagement.Api.IntegrationTests.Purchases;

public class PurchaseOrdersEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PurchaseOrdersEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSendAndReceive_PartialThenComplete_UpdatesStock()
    {
        var (supplierId, productId, salePrice) = await CreateSupplierAndProductAsync("po-flow");
        var created = await CreateOrderAsync(supplierId, productId, 20, 1.25m);

        Assert.Equal(PurchaseOrderStatus.Draft, created.Status);
        Assert.StartsWith("PO-", created.PurchaseOrderNumber);
        Assert.Equal(25.00m, created.Subtotal);
        Assert.Equal(99m, salePrice);
        Assert.Equal(1.25m, created.Items.Single().UnitCost);

        var sent = await SendAsync(created.Id);
        Assert.Equal(PurchaseOrderStatus.Sent, sent.Status);

        var itemId = sent.Items.Single().Id;
        var first = await ReceiveAsync(created.Id, itemId, 8);
        Assert.StartsWith("PR-", first.ReceiptNumber);
        Assert.Equal(1.25m, first.Items.Single().UnitCostSnapshot);

        var afterPartial = await GetOrderAsync(created.Id);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, afterPartial.Status);
        Assert.Equal(8, afterPartial.Items.Single().ReceivedQuantity);
        Assert.Null(afterPartial.ReceivedAtUtc);

        await ReceiveAsync(created.Id, itemId, 12);
        var completed = await GetOrderAsync(created.Id);
        Assert.Equal(PurchaseOrderStatus.Received, completed.Status);
        Assert.Equal(20, completed.Items.Single().ReceivedQuantity);
        Assert.NotNull(completed.ReceivedAtUtc);
        Assert.Equal(20, (await GetBalanceAsync(productId)).QuantityOnHand);

        var receipts = await _client.GetFromJsonAsync<List<PurchaseReceiptResponse>>(
            $"/api/purchase-orders/{created.Id}/receipts",
            JsonOptions);
        Assert.Equal(2, receipts!.Count);
    }

    [Fact]
    public async Task Receive_AbovePending_ReturnsConflictAndLeavesDatabaseUnchanged()
    {
        var (supplierId, productId, _) = await CreateSupplierAndProductAsync("po-over");
        var created = await CreateOrderAsync(supplierId, productId, 20, 2m);
        await SendAsync(created.Id);
        var itemId = created.Items.Single().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{created.Id}/receipts",
            new ReceivePurchaseOrderRequest
            {
                Items = [new ReceivePurchaseOrderItemRequest { PurchaseOrderItemId = itemId, QuantityReceived = 21 }]
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = await GetOrderAsync(created.Id);
        Assert.Equal(PurchaseOrderStatus.Sent, stored.Status);
        Assert.Equal(0, stored.Items.Single().ReceivedQuantity);
        Assert.Equal(0, (await GetBalanceAsync(productId)).QuantityOnHand);

        var receipts = await _client.GetFromJsonAsync<List<PurchaseReceiptResponse>>(
            $"/api/purchase-orders/{created.Id}/receipts",
            JsonOptions);
        Assert.Empty(receipts!);
    }

    [Fact]
    public async Task Cancel_SentOrder_DoesNotChangeStock()
    {
        var (supplierId, productId, _) = await CreateSupplierAndProductAsync("po-cancel");
        var created = await CreateOrderAsync(supplierId, productId, 5, 3m);
        await SendAsync(created.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{created.Id}/cancel",
            new CancelPurchaseOrderRequest { Reason = "No longer needed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelled = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>(JsonOptions);
        Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal(0, (await GetBalanceAsync(productId)).QuantityOnHand);
    }

    [Fact]
    public async Task Cancel_AfterPartialReceipt_KeepsReceivedStock()
    {
        var (supplierId, productId, _) = await CreateSupplierAndProductAsync("po-partial-cancel");
        var created = await CreateOrderAsync(supplierId, productId, 20, 1m);
        await SendAsync(created.Id);
        var itemId = created.Items.Single().Id;
        await ReceiveAsync(created.Id, itemId, 8);

        var response = await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{created.Id}/cancel",
            new CancelPurchaseOrderRequest { Reason = "Stop the rest" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelled = await GetOrderAsync(created.Id);
        Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(8, cancelled.Items.Single().ReceivedQuantity);
        Assert.Equal(8, (await GetBalanceAsync(productId)).QuantityOnHand);
    }

    [Fact]
    public async Task Receive_WithoutPermission_ReturnsForbidden()
    {
        using var factory = new LimitedPurchaseApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/purchase-orders/{Guid.NewGuid()}/receipts",
            new ReceivePurchaseOrderRequest
            {
                Items = [new ReceivePurchaseOrderItemRequest { PurchaseOrderItemId = Guid.NewGuid(), QuantityReceived = 1 }]
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new AnonymousPurchaseApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/purchase-orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Guid SupplierId, Guid ProductId, decimal SalePrice)> CreateSupplierAndProductAsync(string sku)
    {
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
            CurrentPrice = 99m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        return (supplier!.Id, product!.Id, product.CurrentPrice);
    }

    private async Task<PurchaseOrderResponse> CreateOrderAsync(Guid supplierId, Guid productId, int quantity, decimal unitCost)
    {
        var response = await _client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            Items =
            [
                new PurchaseOrderItemRequest { ProductId = productId, OrderedQuantity = quantity, UnitCost = unitCost }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>(JsonOptions))!;
    }

    private async Task<PurchaseOrderResponse> SendAsync(Guid id)
    {
        var response = await _client.PostAsync($"/api/purchase-orders/{id}/send", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>(JsonOptions))!;
    }

    private async Task<PurchaseReceiptResponse> ReceiveAsync(Guid orderId, Guid itemId, int quantity)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{orderId}/receipts",
            new ReceivePurchaseOrderRequest
            {
                Items = [new ReceivePurchaseOrderItemRequest { PurchaseOrderItemId = itemId, QuantityReceived = quantity }]
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseReceiptResponse>(JsonOptions))!;
    }

    private async Task<PurchaseOrderResponse> GetOrderAsync(Guid id)
    {
        return (await _client.GetFromJsonAsync<PurchaseOrderResponse>($"/api/purchase-orders/{id}", JsonOptions))!;
    }

    private async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId)
    {
        return (await _client.GetFromJsonAsync<InventoryBalanceResponse>($"/api/inventory/balances/{productId}", JsonOptions))!;
    }
}
