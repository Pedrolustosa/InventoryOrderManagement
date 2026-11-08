using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryOrderManagement.Api.IntegrationTests.Shipments;

public class ShipmentsEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ShipmentsEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDispatchDeliver_SyncsOrderStatus_AndWritesHistory()
    {
        var order = await SeedConfirmedOrderAsync("ship-happy");

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = order.Id,
            CarrierName = "Acme Logistics",
            TrackingCode = "TRK-100",
            Notes = "Ready"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        Assert.Equal(ShipmentStatus.Preparing, created!.Status);
        Assert.Equal(order.OrderNumber, created.OrderNumber);
        Assert.Single(created.History);

        var orderStillConfirmed = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Confirmed, orderStillConfirmed!.Status);

        var dispatchResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/dispatch",
            new ShipmentNotesRequest { Notes = "Left warehouse" });
        dispatchResponse.EnsureSuccessStatusCode();
        var dispatched = await dispatchResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        Assert.Equal(ShipmentStatus.Shipped, dispatched!.Status);
        Assert.NotNull(dispatched.ShippedAtUtc);

        var orderShipped = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Shipped, orderShipped!.Status);

        (await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/in-transit",
            new ShipmentNotesRequest { Notes = "Hub departure" })).EnsureSuccessStatusCode();

        var deliverResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/deliver",
            new ShipmentNotesRequest { Notes = "Delivered to customer" });
        deliverResponse.EnsureSuccessStatusCode();
        var delivered = await deliverResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        Assert.Equal(ShipmentStatus.Delivered, delivered!.Status);
        Assert.NotNull(delivered.DeliveredAtUtc);
        Assert.True(delivered.History.Count >= 4);

        var orderDelivered = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Delivered, orderDelivered!.Status);

        var history = await _client.GetFromJsonAsync<List<ShipmentStatusHistoryResponse>>(
            $"/api/shipments/{created.Id}/history",
            JsonOptions);
        Assert.Equal(delivered.History.Count, history!.Count);
    }

    [Fact]
    public async Task Dispatch_IssuesStock_CreatesReceivable_AndShipsOrder()
    {
        var order = await SeedConfirmedOrderAsync("ship-stock-fin");
        var productId = order.Items.Single().ProductId;

        var reserved = await GetBalanceAsync(productId);
        Assert.Equal(10, reserved.QuantityOnHand);
        Assert.Equal(2, reserved.ReservedQuantity);

        var created = await CreateShipmentAsync(order.Id, "Acme");
        var dispatchResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/dispatch",
            new ShipmentNotesRequest());
        Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);

        var balance = await GetBalanceAsync(productId);
        Assert.Equal(8, balance.QuantityOnHand);
        Assert.Equal(0, balance.ReservedQuantity);
        Assert.Equal(8, balance.AvailableQuantity);

        var movements = await _client.GetFromJsonAsync<List<StockMovementResponse>>(
            $"/api/inventory/products/{productId}/movements",
            JsonOptions);
        var exit = Assert.Single(movements!, movement => movement.MovementType == StockMovementType.OrderExit);
        Assert.Equal(order.Id, exit.OrderId);
        Assert.Equal(2, exit.Quantity);

        var receivables = await GetReceivablesForOrderAsync(order.Id);
        var title = Assert.Single(receivables.Items);
        Assert.Equal(order.Subtotal, title.TotalAmount);
        Assert.Equal(order.CustomerId, title.CounterpartyId);
        Assert.Equal(FinancialTitleStatus.Pending, title.Status);
        Assert.Equal(title.IssueDate.AddDays(30), title.DueDate);

        var shippedOrder = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Shipped, shippedOrder!.Status);

        var repeat = await _client.PostAsJsonAsync($"/api/shipments/{created.Id}/dispatch", new ShipmentNotesRequest());
        Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode);
        Assert.Single((await GetReceivablesForOrderAsync(order.Id)).Items);
        Assert.Equal(8, (await GetBalanceAsync(productId)).QuantityOnHand);
    }

    [Fact]
    public async Task Dispatch_WhenReceivableCreationFails_RollsBackEverything()
    {
        var order = await SeedConfirmedOrderAsync("ship-rollback");
        var productId = order.Items.Single().ProductId;
        var created = await CreateShipmentAsync(order.Id, "Acme");

        // A manual receivable already uses the document number the dispatch would generate.
        (await _client.PostAsJsonAsync("/api/finance/receivables", new CreateReceivableRequest
        {
            Description = "Manual receivable",
            CustomerId = order.CustomerId,
            SourceType = FinancialSourceType.Manual,
            DocumentNumber = $"SO-{order.OrderNumber}",
            IssueDate = new DateOnly(2026, 11, 1),
            TotalAmount = 10m,
            Installments =
            [
                new FinancialInstallmentRequest { InstallmentNumber = 1, DueDate = new DateOnly(2026, 12, 1), Amount = 10m }
            ]
        })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/dispatch",
            new ShipmentNotesRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(JsonOptions);
        Assert.Equal((int)HttpStatusCode.Conflict, problem!.Status);
        Assert.Contains($"SO-{order.OrderNumber}", problem.Detail);

        var balance = await GetBalanceAsync(productId);
        Assert.Equal(10, balance.QuantityOnHand);
        Assert.Equal(2, balance.ReservedQuantity);

        var movements = await _client.GetFromJsonAsync<List<StockMovementResponse>>(
            $"/api/inventory/products/{productId}/movements",
            JsonOptions);
        Assert.DoesNotContain(movements!, movement => movement.MovementType == StockMovementType.OrderExit);

        Assert.Empty((await GetReceivablesForOrderAsync(order.Id)).Items);

        var shipment = await _client.GetFromJsonAsync<ShipmentResponse>($"/api/shipments/{created.Id}", JsonOptions);
        Assert.Equal(ShipmentStatus.Preparing, shipment!.Status);
        Assert.Null(shipment.ShippedAtUtc);

        var unchangedOrder = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Confirmed, unchangedOrder!.Status);
    }

    [Fact]
    public async Task Create_DuplicateForSameOrder_ReturnsConflict()
    {
        var order = await SeedConfirmedOrderAsync("ship-dup");
        (await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = order.Id,
            CarrierName = "Carrier A"
        })).EnsureSuccessStatusCode();

        var duplicate = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = order.Id,
            CarrierName = "Carrier B"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Create_FromDraftOrder_ReturnsConflict()
    {
        var order = await SeedDraftOrderAsync("ship-draft");

        var response = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = order.Id,
            CarrierName = "Carrier"
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FailAndResume_KeepsOrderShipped()
    {
        var order = await SeedConfirmedOrderAsync("ship-fail");
        var created = await CreateShipmentAsync(order.Id, "Acme");
        (await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/dispatch",
            new ShipmentNotesRequest())).EnsureSuccessStatusCode();

        var failResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/fail",
            new FailShipmentRequest { FailureReason = "Customer unavailable" });
        failResponse.EnsureSuccessStatusCode();
        var failed = await failResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions);
        Assert.Equal(ShipmentStatus.Failed, failed!.Status);

        var orderAfterFail = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions);
        Assert.Equal(OrderStatus.Shipped, orderAfterFail!.Status);

        (await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/resume",
            new ShipmentNotesRequest { Notes = "Retry" })).EnsureSuccessStatusCode();
        var resumed = await _client.GetFromJsonAsync<ShipmentResponse>($"/api/shipments/{created.Id}", JsonOptions);
        Assert.Equal(ShipmentStatus.InTransit, resumed!.Status);
        Assert.Null(resumed.FailureReason);
    }

    [Fact]
    public async Task OrderShip_WhenShipmentExists_ReturnsConflict()
    {
        var order = await SeedConfirmedOrderAsync("ship-gate");
        await CreateShipmentAsync(order.Id, "Acme");

        var response = await _client.PostAsync($"/api/orders/{order.Id}/ship", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Dispatch_WithoutCarrier_ReturnsBadRequest()
    {
        var order = await SeedConfirmedOrderAsync("ship-nocarrier");
        var created = await CreateShipmentAsync(order.Id, carrierName: null);

        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/dispatch",
            new ShipmentNotesRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var order = await SeedConfirmedOrderAsync("ship-list");
        var created = await CreateShipmentAsync(order.Id, "List Carrier");

        var page = await _client.GetFromJsonAsync<PagedResult<ShipmentSummaryResponse>>(
            "/api/shipments?status=Preparing&page=1&pageSize=50",
            JsonOptions);
        Assert.Contains(page!.Items, item => item.Id == created.Id);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new AnonymousShipmentsApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/shipments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId) =>
        (await _client.GetFromJsonAsync<InventoryBalanceResponse>(
            $"/api/inventory/balances/{productId}",
            JsonOptions))!;

    private async Task<PagedResult<FinancialTitleSummaryResponse>> GetReceivablesForOrderAsync(Guid orderId) =>
        (await _client.GetFromJsonAsync<PagedResult<FinancialTitleSummaryResponse>>(
            $"/api/finance/receivables?sourceId={orderId}&page=1&pageSize=10",
            JsonOptions))!;

    private async Task<ShipmentResponse> CreateShipmentAsync(Guid orderId, string? carrierName)
    {
        var response = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = orderId,
            CarrierName = carrierName
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShipmentResponse>(JsonOptions))!;
    }

    private async Task<OrderResponse> SeedConfirmedOrderAsync(string sku)
    {
        var orderId = await CreateOrderThroughConfirmAsync(sku);
        return (await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", JsonOptions))!;
    }

    private async Task<OrderCreatedResponse> SeedDraftOrderAsync(string sku)
    {
        var (_, productId, customerId) = await SeedCatalogAsync(sku);
        var response = await _client.PostAsJsonAsync("/api/orders", BuildOrderRequest(customerId, productId, 1));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderCreatedResponse>(JsonOptions))!;
    }

    private async Task<Guid> CreateOrderThroughConfirmAsync(string sku)
    {
        var (_, productId, customerId) = await SeedCatalogAsync(sku);
        var response = await _client.PostAsJsonAsync("/api/orders", BuildOrderRequest(customerId, productId, 2));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<OrderCreatedResponse>(JsonOptions);
        (await _client.PostAsync($"/api/orders/{created!.Id}/confirm", null)).EnsureSuccessStatusCode();
        return created.Id;
    }

    private async Task<(Guid SupplierId, Guid ProductId, Guid CustomerId)> SeedCatalogAsync(string sku)
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
            CurrentPrice = 25m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        var purchaseResponse = await _client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplier!.Id,
            Items =
            [
                new PurchaseOrderItemRequest
                {
                    ProductId = product!.Id,
                    OrderedQuantity = 10,
                    UnitCost = 10m
                }
            ]
        });
        purchaseResponse.EnsureSuccessStatusCode();
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>(JsonOptions);
        (await _client.PostAsync($"/api/purchase-orders/{purchase!.Id}/send", null)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{purchase.Id}/receipts",
            new ReceivePurchaseOrderRequest
            {
                Items =
                [
                    new ReceivePurchaseOrderItemRequest
                    {
                        PurchaseOrderItemId = purchase.Items.Single().Id,
                        QuantityReceived = 10
                    }
                ]
            })).EnsureSuccessStatusCode();

        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = $"Customer {sku}"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        return (supplier.Id, product.Id, customer!.Id);
    }

    private static CreateOrderRequest BuildOrderRequest(Guid customerId, Guid productId, int quantity)
        => new()
        {
            CustomerId = customerId,
            ShippingAddress = new OrderShippingAddressRequest
            {
                PostalCode = "01000-000",
                Street = "Main",
                Number = "1",
                City = "Sao Paulo",
                State = "SP",
                Country = "BR"
            },
            Items = [new OrderItemRequest { ProductId = productId, Quantity = quantity }]
        };
}

public sealed class AnonymousShipmentsApiFactory : ProductsApiFactory
{
    protected override bool UseTestAuthentication => false;
}
