using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Api.IntegrationTests.Returns;

public class ReturnsEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ReturnsEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CustomerReturn_PartialRestock_IncreasesStockAndBlocksOverReturn()
    {
        var (customerId, productId) = await SeedDeliveredSaleAsync("ret-cust", quantity: 10);
        var order = await GetLatestDeliveredOrderAsync(customerId);
        var orderItemId = order.Items.Single().Id;
        var balanceBefore = await GetBalanceAsync(productId);

        var createdResponse = await _client.PostAsJsonAsync("/api/returns/customer", new CreateCustomerReturnRequest
        {
            OrderId = order.Id,
            Reason = ReturnReason.Defective,
            FinancialTreatment = ReturnFinancialTreatment.CustomerRefund,
            Items = [new CreateCustomerReturnItemRequest { OrderItemId = orderItemId, RequestedQuantity = 3 }]
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);
        Assert.StartsWith("RET-", created!.ReturnNumber);
        Assert.Equal(ReturnStatus.Draft, created.Status);

        var receiveResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/receive",
            new ReceiveCustomerReturnRequest
            {
                Items = [new ReceiveCustomerReturnItemRequest { ReturnItemId = created.Items.Single().Id, ReceivedQuantity = 3 }]
            });
        receiveResponse.EnsureSuccessStatusCode();

        await _client.PostAsync($"/api/returns/{created.Id}/inspection/start", null);

        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/inspection/complete",
            new CompleteReturnInspectionRequest
            {
                Items =
                [
                    new CompleteReturnInspectionItemRequest
                    {
                        ReturnItemId = created.Items.Single().Id,
                        Condition = ReturnItemCondition.Good,
                        RestockedQuantity = 3,
                        QuarantinedQuantity = 0,
                        DiscardedQuantity = 0,
                        RejectedQuantity = 0
                    }
                ]
            });
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);
        Assert.Equal(ReturnStatus.Completed, completed!.Status);

        var balanceAfter = await GetBalanceAsync(productId);
        Assert.Equal(balanceBefore.QuantityOnHand + 3, balanceAfter.QuantityOnHand);

        var movements = await _client.GetFromJsonAsync<List<ReturnStockMovementResponse>>(
            $"/api/returns/{created.Id}/stock-movements",
            JsonOptions);
        Assert.Single(movements!);
        Assert.Equal(StockMovementType.CustomerReturnEntry, movements[0].MovementType);
        Assert.Equal(3, movements[0].Quantity);

        var over = await _client.PostAsJsonAsync("/api/returns/customer", new CreateCustomerReturnRequest
        {
            OrderId = order.Id,
            Reason = ReturnReason.CustomerRegret,
            Items = [new CreateCustomerReturnItemRequest { OrderItemId = orderItemId, RequestedQuantity = 8 }]
        });
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);
    }

    [Fact]
    public async Task CustomerReturn_MixedDisposition_RestocksOnlyApprovedUnits()
    {
        var (customerId, productId) = await SeedDeliveredSaleAsync("ret-mix", quantity: 5);
        var order = await GetLatestDeliveredOrderAsync(customerId);
        var balanceBefore = await GetBalanceAsync(productId);

        var created = await CreateCustomerReturnAsync(order.Id, order.Items.Single().Id, 5);
        await ReceiveAsync(created.Id, created.Items.Single().Id, 5);
        await _client.PostAsync($"/api/returns/{created.Id}/inspection/start", null);

        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/inspection/complete",
            new CompleteReturnInspectionRequest
            {
                Items =
                [
                    new CompleteReturnInspectionItemRequest
                    {
                        ReturnItemId = created.Items.Single().Id,
                        Condition = ReturnItemCondition.Damaged,
                        RestockedQuantity = 2,
                        QuarantinedQuantity = 2,
                        DiscardedQuantity = 1,
                        RejectedQuantity = 0
                    }
                ]
            });
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);

        Assert.Equal(ReturnStatus.Completed, completed!.Status);
        Assert.Equal(2, completed.Items.Single().RestockedQuantity);
        Assert.Equal(balanceBefore.QuantityOnHand + 2, (await GetBalanceAsync(productId)).QuantityOnHand);
    }

    [Fact]
    public async Task CustomerReturn_AllRejected_DoesNotChangeStock()
    {
        var (customerId, productId) = await SeedDeliveredSaleAsync("ret-rej", quantity: 2);
        var order = await GetLatestDeliveredOrderAsync(customerId);
        var balanceBefore = await GetBalanceAsync(productId);

        var created = await CreateCustomerReturnAsync(order.Id, order.Items.Single().Id, 2);
        await ReceiveAsync(created.Id, created.Items.Single().Id, 2);
        await _client.PostAsync($"/api/returns/{created.Id}/inspection/start", null);

        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/inspection/complete",
            new CompleteReturnInspectionRequest
            {
                Items =
                [
                    new CompleteReturnInspectionItemRequest
                    {
                        ReturnItemId = created.Items.Single().Id,
                        Condition = ReturnItemCondition.Defective,
                        RestockedQuantity = 0,
                        QuarantinedQuantity = 0,
                        DiscardedQuantity = 0,
                        RejectedQuantity = 2
                    }
                ]
            });
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);

        Assert.Equal(ReturnStatus.Rejected, completed!.Status);
        Assert.Equal(balanceBefore.QuantityOnHand, (await GetBalanceAsync(productId)).QuantityOnHand);
        var movements = await _client.GetFromJsonAsync<List<ReturnStockMovementResponse>>(
            $"/api/returns/{created.Id}/stock-movements",
            JsonOptions);
        Assert.Empty(movements!);
    }

    [Fact]
    public async Task SupplierReturn_Dispatch_ReducesStock()
    {
        var (supplierId, productId) = await CreateSupplierAndProductAsync("ret-sup");
        var purchase = await CreatePurchaseAsync(supplierId, productId, 20, 1.5m);
        await SendPurchaseAsync(purchase.Id);
        await ReceivePurchaseAsync(purchase.Id, purchase.Items.Single().Id, 15);
        var balanceBefore = await GetBalanceAsync(productId);

        var createdResponse = await _client.PostAsJsonAsync("/api/returns/supplier", new CreateSupplierReturnRequest
        {
            PurchaseOrderId = purchase.Id,
            Reason = ReturnReason.Damaged,
            Items =
            [
                new CreateSupplierReturnItemRequest
                {
                    PurchaseOrderItemId = purchase.Items.Single().Id,
                    RequestedQuantity = 5
                }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);

        await _client.PostAsJsonAsync(
            $"/api/returns/{created!.Id}/authorize",
            new AuthorizeSupplierReturnRequest { AuthorizationReference = "AUTH-1" });

        var dispatchResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/dispatch",
            new DispatchSupplierReturnRequest { DispatchReference = "NF-1" });
        dispatchResponse.EnsureSuccessStatusCode();
        var dispatched = await dispatchResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);

        Assert.Equal(ReturnStatus.Dispatched, dispatched!.Status);
        Assert.Equal(balanceBefore.QuantityOnHand - 5, (await GetBalanceAsync(productId)).QuantityOnHand);

        var over = await _client.PostAsJsonAsync("/api/returns/supplier", new CreateSupplierReturnRequest
        {
            PurchaseOrderId = purchase.Id,
            Reason = ReturnReason.QualityIssue,
            Items =
            [
                new CreateSupplierReturnItemRequest
                {
                    PurchaseOrderItemId = purchase.Items.Single().Id,
                    RequestedQuantity = 11
                }
            ]
        });
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);
    }

    [Fact]
    public async Task SupplierReturn_DispatchWithoutStock_ReturnsConflict()
    {
        var (supplierId, productId) = await CreateSupplierAndProductAsync("ret-nostock");
        var purchase = await CreatePurchaseAsync(supplierId, productId, 8, 2m);
        await SendPurchaseAsync(purchase.Id);
        await ReceivePurchaseAsync(purchase.Id, purchase.Items.Single().Id, 8);

        await _client.PostAsJsonAsync("/api/inventory/exits", new RegisterStockExitRequest
        {
            ProductId = productId,
            Quantity = 5,
            Reason = "Consume before return"
        });

        var created = await CreateSupplierReturnAsync(purchase.Id, purchase.Items.Single().Id, 8);
        await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/authorize",
            new AuthorizeSupplierReturnRequest());

        var dispatchResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/dispatch",
            new DispatchSupplierReturnRequest());

        Assert.Equal(HttpStatusCode.Conflict, dispatchResponse.StatusCode);
        var stored = await _client.GetFromJsonAsync<ReturnResponse>($"/api/returns/{created.Id}", JsonOptions);
        Assert.Equal(ReturnStatus.Authorized, stored!.Status);
    }

    [Fact]
    public async Task Cancel_Draft_DoesNotChangeStock()
    {
        var (customerId, productId) = await SeedDeliveredSaleAsync("ret-cancel", quantity: 4);
        var order = await GetLatestDeliveredOrderAsync(customerId);
        var balanceBefore = await GetBalanceAsync(productId);

        var created = await CreateCustomerReturnAsync(order.Id, order.Items.Single().Id, 2);
        var cancelResponse = await _client.PostAsJsonAsync(
            $"/api/returns/{created.Id}/cancel",
            new CancelReturnRequest { Reason = "No longer needed" });

        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions);
        Assert.Equal(ReturnStatus.Cancelled, cancelled!.Status);
        Assert.Equal(balanceBefore.QuantityOnHand, (await GetBalanceAsync(productId)).QuantityOnHand);
    }

    [Fact]
    public async Task CompleteInspection_Twice_ReturnsConflict()
    {
        var (customerId, _) = await SeedDeliveredSaleAsync("ret-twice", quantity: 1);
        var order = await GetLatestDeliveredOrderAsync(customerId);
        var created = await CreateCustomerReturnAsync(order.Id, order.Items.Single().Id, 1);
        await ReceiveAsync(created.Id, created.Items.Single().Id, 1);
        await _client.PostAsync($"/api/returns/{created.Id}/inspection/start", null);

        var body = new CompleteReturnInspectionRequest
        {
            Items =
            [
                new CompleteReturnInspectionItemRequest
                {
                    ReturnItemId = created.Items.Single().Id,
                    Condition = ReturnItemCondition.Good,
                    RestockedQuantity = 1,
                    QuarantinedQuantity = 0,
                    DiscardedQuantity = 0,
                    RejectedQuantity = 0
                }
            ]
        };

        (await _client.PostAsJsonAsync($"/api/returns/{created.Id}/inspection/complete", body)).EnsureSuccessStatusCode();
        var second = await _client.PostAsJsonAsync($"/api/returns/{created.Id}/inspection/complete", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Dispatch_WithoutPermission_ReturnsForbidden()
    {
        using var factory = new LimitedReturnsApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/{Guid.NewGuid()}/dispatch",
            new DispatchSupplierReturnRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new AnonymousReturnsApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/returns");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Guid CustomerId, Guid ProductId)> SeedDeliveredSaleAsync(string sku, int quantity)
    {
        var (supplierId, productId) = await CreateSupplierAndProductAsync(sku);
        var purchase = await CreatePurchaseAsync(supplierId, productId, quantity, 1m);
        await SendPurchaseAsync(purchase.Id);
        await ReceivePurchaseAsync(purchase.Id, purchase.Items.Single().Id, quantity);

        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = $"Customer {sku}"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = customer!.Id,
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
        });
        orderResponse.EnsureSuccessStatusCode();
        var created = await orderResponse.Content.ReadFromJsonAsync<OrderCreatedResponse>(JsonOptions);

        (await _client.PostAsync($"/api/orders/{created!.Id}/confirm", null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/orders/{created.Id}/ship", null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/orders/{created.Id}/deliver", null)).EnsureSuccessStatusCode();

        return (customer.Id, productId);
    }

    private async Task<OrderResponse> GetLatestDeliveredOrderAsync(Guid customerId)
    {
        var orders = await _client.GetFromJsonAsync<List<OrderSummaryResponse>>(
            $"/api/orders?customerId={customerId}&status=Delivered",
            JsonOptions);
        var summary = orders!.Single();
        return (await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{summary.Id}", JsonOptions))!;
    }

    private async Task<ReturnResponse> CreateCustomerReturnAsync(Guid orderId, Guid orderItemId, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/returns/customer", new CreateCustomerReturnRequest
        {
            OrderId = orderId,
            Reason = ReturnReason.Defective,
            Items = [new CreateCustomerReturnItemRequest { OrderItemId = orderItemId, RequestedQuantity = quantity }]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions))!;
    }

    private async Task ReceiveAsync(Guid returnId, Guid returnItemId, int quantity)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/returns/{returnId}/receive",
            new ReceiveCustomerReturnRequest
            {
                Items = [new ReceiveCustomerReturnItemRequest { ReturnItemId = returnItemId, ReceivedQuantity = quantity }]
            });
        response.EnsureSuccessStatusCode();
    }

    private async Task<ReturnResponse> CreateSupplierReturnAsync(Guid purchaseOrderId, Guid itemId, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/returns/supplier", new CreateSupplierReturnRequest
        {
            PurchaseOrderId = purchaseOrderId,
            Reason = ReturnReason.Damaged,
            Items =
            [
                new CreateSupplierReturnItemRequest
                {
                    PurchaseOrderItemId = itemId,
                    RequestedQuantity = quantity
                }
            ]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReturnResponse>(JsonOptions))!;
    }

    private async Task<(Guid SupplierId, Guid ProductId)> CreateSupplierAndProductAsync(string sku)
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
            CurrentPrice = 50m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        return (supplier!.Id, product!.Id);
    }

    private async Task<PurchaseOrderResponse> CreatePurchaseAsync(Guid supplierId, Guid productId, int quantity, decimal unitCost)
    {
        var response = await _client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplierId,
            Items =
            [
                new PurchaseOrderItemRequest { ProductId = productId, OrderedQuantity = quantity, UnitCost = unitCost }
            ]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>(JsonOptions))!;
    }

    private async Task SendPurchaseAsync(Guid id)
    {
        (await _client.PostAsync($"/api/purchase-orders/{id}/send", null)).EnsureSuccessStatusCode();
    }

    private async Task ReceivePurchaseAsync(Guid orderId, Guid itemId, int quantity)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/purchase-orders/{orderId}/receipts",
            new ReceivePurchaseOrderRequest
            {
                Items = [new ReceivePurchaseOrderItemRequest { PurchaseOrderItemId = itemId, QuantityReceived = quantity }]
            });
        response.EnsureSuccessStatusCode();
    }

    private async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId)
        => (await _client.GetFromJsonAsync<InventoryBalanceResponse>($"/api/inventory/balances/{productId}", JsonOptions))!;
}

public sealed class LimitedReturnsApiFactory : ProductsApiFactory
{
    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = LimitedReturnsAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = LimitedReturnsAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, LimitedReturnsAuthenticationHandler>(
            LimitedReturnsAuthenticationHandler.SchemeName,
            _ => { });
    }
}

public sealed class AnonymousReturnsApiFactory : ProductsApiFactory
{
    protected override bool UseTestAuthentication => false;
}

public sealed class LimitedReturnsAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTestReturnsLimited";

    public LimitedReturnsAuthenticationHandler(
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
            new(ClaimTypes.Name, "integration-returns@localhost"),
            new(ClaimTypes.Role, SystemRoles.Viewer)
        };
        claims.AddRange(Permissions.All
            .Where(permission => permission != Permissions.Returns.SupplierDispatch)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
