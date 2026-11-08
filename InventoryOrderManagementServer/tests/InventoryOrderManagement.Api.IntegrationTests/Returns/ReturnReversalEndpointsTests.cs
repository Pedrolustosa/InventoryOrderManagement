using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryOrderManagement.Api.IntegrationTests.Returns;

public class ReturnReversalEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private readonly ReturnFlowApi _api;

    public ReturnReversalEndpointsTests(ProductsApiFactory factory)
    {
        _api = new ReturnFlowApi(factory.CreateClient());
    }

    [Fact]
    public async Task CustomerReturn_FullFlow_RestocksQuarantinesAndAbatesTheReceivable()
    {
        // 4 units at 50.00 dispatched through a shipment, so the order has a 200.00 receivable.
        var sale = await _api.SeedDeliveredSaleAsync("rev-flow", quantity: 4, throughShipment: true);
        var receivables = await _api.GetTitlesAsync("receivables", sale.Order.Id);
        var receivable = Assert.Single(receivables.Items);
        Assert.Equal(200m, receivable.TotalAmount);

        var created = await _api.CreateCustomerReturnAsync(sale.Order, quantity: 3, ReturnFinancialTreatment.CustomerRefund);
        var itemId = created.Items.Single().Id;
        await _api.ReceiveAndStartInspectionAsync(created.Id, itemId, 3);

        var complete = await _api.CompleteInspectionAsync(created.Id, itemId, restocked: 2, quarantined: 1);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var completed = (await complete.Content.ReadFromJsonAsync<ReturnResponse>(ReturnFlowApi.JsonOptions))!;
        Assert.Equal(ReturnStatus.Completed, completed.Status);
        Assert.Equal(ReturnFinancialStatus.Processed, completed.FinancialStatus);
        Assert.Equal(receivable.Id, completed.AdjustedFinancialTitleId);
        Assert.Equal(150m, completed.FinancialAdjustedAmount);
        Assert.Null(completed.FinancialTitleId);

        // Stock: 10 received − 4 sold + 2 restocked. The quarantined unit is recorded, not available.
        var balance = await _api.GetBalanceAsync(sale.ProductId);
        Assert.Equal(8, balance.QuantityOnHand);
        Assert.Equal(0, balance.ReservedQuantity);
        Assert.Equal(8, balance.AvailableQuantity);

        var movements = await _api.GetReturnMovementsAsync(created.Id);
        Assert.Equal(2, movements.Single(m => m.MovementType == StockMovementType.CustomerReturnEntry).Quantity);
        var quarantine = movements.Single(m => m.MovementType == StockMovementType.CustomerReturnQuarantine);
        Assert.Equal(1, quarantine.Quantity);
        Assert.Equal(quarantine.PreviousQuantity, quarantine.NewQuantity);

        // Finance: the open receivable is abated by a cash-less return credit; no refund title.
        var abated = await _api.GetTitleAsync("receivables", receivable.Id);
        Assert.Equal(50m, abated.OutstandingBalance);
        var credit = Assert.Single(abated.Settlements);
        Assert.Equal(FinancialPaymentMethod.ReturnCredit, credit.PaymentMethod);
        Assert.Equal(150m, credit.SettledPrincipalAmount);
        Assert.Equal(0m, credit.CashAmount);
        Assert.Empty((await _api.GetTitlesAsync("payables", created.Id)).Items);

        // Completing again is rejected and duplicates nothing.
        var repeat = await _api.CompleteInspectionAsync(created.Id, itemId, restocked: 2, quarantined: 1);
        Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode);
        Assert.Single((await _api.GetTitleAsync("receivables", receivable.Id)).Settlements);
        Assert.Equal(8, (await _api.GetBalanceAsync(sale.ProductId)).QuantityOnHand);
    }
}

public class ReturnReversalRollbackTests : IClassFixture<FailingReturnFinanceApiFactory>
{
    private readonly ReturnFlowApi _api;

    public ReturnReversalRollbackTests(FailingReturnFinanceApiFactory factory)
    {
        _api = new ReturnFlowApi(factory.CreateClient());
    }

    [Fact]
    public async Task CustomerReturn_WhenFinanceFails_RollsBackStockOrderAndStatus()
    {
        // Legacy ship/deliver: no receivable, so the refund needs a new payable, whose creation fails.
        var sale = await _api.SeedDeliveredSaleAsync("rev-rollback", quantity: 4, throughShipment: false);
        var created = await _api.CreateCustomerReturnAsync(sale.Order, quantity: 2, ReturnFinancialTreatment.CustomerRefund);
        var itemId = created.Items.Single().Id;
        await _api.ReceiveAndStartInspectionAsync(created.Id, itemId, 2);
        var balanceBefore = await _api.GetBalanceAsync(sale.ProductId);

        var response = await _api.CompleteInspectionAsync(created.Id, itemId, restocked: 2, quarantined: 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(ReturnFlowApi.JsonOptions);
        Assert.Equal((int)HttpStatusCode.Conflict, problem!.Status);

        var balanceAfter = await _api.GetBalanceAsync(sale.ProductId);
        Assert.Equal(balanceBefore.QuantityOnHand, balanceAfter.QuantityOnHand);
        Assert.Equal(balanceBefore.ReservedQuantity, balanceAfter.ReservedQuantity);
        Assert.Empty(await _api.GetReturnMovementsAsync(created.Id));
        Assert.Empty((await _api.GetTitlesAsync("payables", created.Id)).Items);

        var unchanged = await _api.GetReturnAsync(created.Id);
        Assert.Equal(ReturnStatus.UnderInspection, unchanged.Status);
        Assert.Equal(ReturnFinancialStatus.Pending, unchanged.FinancialStatus);

        var eligible = await _api.GetEligibleItemsAsync(sale.Order.Id);
        Assert.Equal(0, eligible.Single().PreviouslyReturnedQuantity);
    }
}

/// <summary>Fails when the reversal tries to create the return's financial title.</summary>
public sealed class FailingReturnFinanceApiFactory : ProductsApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFinancialTitleRepository>();
            services.AddScoped<FinancialTitleRepository>();
            services.AddScoped<IFinancialTitleRepository>(provider =>
                new FailingReturnTitleRepository(provider.GetRequiredService<FinancialTitleRepository>()));
        });
    }

    private sealed class FailingReturnTitleRepository(IFinancialTitleRepository inner) : IFinancialTitleRepository
    {
        public Task AddAsync(FinancialTitle title, CancellationToken cancellationToken = default) =>
            title.SourceType == FinancialSourceType.Return
                ? throw new FinancialConflictException("Simulated failure creating the return title.")
                : inner.AddAsync(title, cancellationToken);

        public Task<FinancialTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            inner.GetByIdAsync(id, cancellationToken);

        public Task<FinancialTitle?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            inner.GetTrackedByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNumberAsync(string titleNumber, CancellationToken cancellationToken = default) =>
            inner.ExistsByNumberAsync(titleNumber, cancellationToken);

        public Task<bool> ExistsReceivableForSalesOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            inner.ExistsReceivableForSalesOrderAsync(orderId, cancellationToken);

        public Task<bool> DocumentExistsAsync(FinancialTitle title, CancellationToken cancellationToken = default) =>
            inner.DocumentExistsAsync(title, cancellationToken);

        public Task<FinancialTitle?> FindActiveSourceTitleAsync(
            FinancialTitleType type,
            FinancialSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            inner.FindActiveSourceTitleAsync(type, sourceType, sourceId, cancellationToken);

        public Task<bool> ExistsForSourceAsync(
            FinancialSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            inner.ExistsForSourceAsync(sourceType, sourceId, cancellationToken);

        public Task<IReadOnlyList<FinancialTitle>> ListAsync(
            FinancialTitleListFilter filter,
            CancellationToken cancellationToken = default) =>
            inner.ListAsync(filter, cancellationToken);

        public Task<IReadOnlyList<FinancialInstallmentRow>> ListInstallmentsAsync(CancellationToken cancellationToken = default) =>
            inner.ListInstallmentsAsync(cancellationToken);

        public void RemoveInstallments(IReadOnlyCollection<FinancialInstallment> installments) =>
            inner.RemoveInstallments(installments);

        public void StageSettlement(FinancialSettlement settlement) => inner.StageSettlement(settlement);
    }
}

/// <summary>HTTP helpers that drive a sale and a customer return through the API.</summary>
internal sealed class ReturnFlowApi(HttpClient client)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public sealed record Sale(OrderResponse Order, Guid ProductId);

    public async Task<Sale> SeedDeliveredSaleAsync(string sku, int quantity, bool throughShipment)
    {
        var supplier = await PostAsync<SupplierResponse>("/api/suppliers", new CreateSupplierRequest { Name = $"Supplier {sku}" });
        var product = await PostAsync<ProductResponse>("/api/products", new CreateProductRequest
        {
            Sku = sku,
            Name = $"Product {sku}",
            CurrentPrice = 50m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });

        var purchase = await PostAsync<PurchaseOrderResponse>("/api/purchase-orders", new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = [new PurchaseOrderItemRequest { ProductId = product.Id, OrderedQuantity = 10, UnitCost = 10m }]
        });
        (await client.PostAsync($"/api/purchase-orders/{purchase.Id}/send", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/purchase-orders/{purchase.Id}/receipts", new ReceivePurchaseOrderRequest
        {
            Items = [new ReceivePurchaseOrderItemRequest { PurchaseOrderItemId = purchase.Items.Single().Id, QuantityReceived = 10 }]
        })).EnsureSuccessStatusCode();

        var customer = await PostAsync<CustomerResponse>("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = $"Customer {sku}"
        });

        var order = await PostAsync<OrderCreatedResponse>("/api/orders", new CreateOrderRequest
        {
            CustomerId = customer.Id,
            ShippingAddress = new OrderShippingAddressRequest
            {
                PostalCode = "01000-000",
                Street = "Main",
                Number = "1",
                City = "Sao Paulo",
                State = "SP",
                Country = "BR"
            },
            Items = [new OrderItemRequest { ProductId = product.Id, Quantity = quantity }]
        });
        (await client.PostAsync($"/api/orders/{order.Id}/confirm", null)).EnsureSuccessStatusCode();

        if (throughShipment)
        {
            var shipment = await PostAsync<ShipmentResponse>("/api/shipments", new CreateShipmentRequest
            {
                OrderId = order.Id,
                CarrierName = "Acme"
            });
            foreach (var step in new[] { "dispatch", "in-transit", "deliver" })
            {
                (await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/{step}", new ShipmentNotesRequest()))
                    .EnsureSuccessStatusCode();
            }
        }
        else
        {
            (await client.PostAsync($"/api/orders/{order.Id}/ship", null)).EnsureSuccessStatusCode();
            (await client.PostAsync($"/api/orders/{order.Id}/deliver", null)).EnsureSuccessStatusCode();
        }

        var delivered = (await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", JsonOptions))!;
        return new Sale(delivered, product.Id);
    }

    public Task<ReturnResponse> CreateCustomerReturnAsync(
        OrderResponse order,
        int quantity,
        ReturnFinancialTreatment treatment) =>
        PostAsync<ReturnResponse>("/api/returns/customer", new CreateCustomerReturnRequest
        {
            OrderId = order.Id,
            Reason = ReturnReason.Defective,
            FinancialTreatment = treatment,
            Items = [new CreateCustomerReturnItemRequest { OrderItemId = order.Items.Single().Id, RequestedQuantity = quantity }]
        });

    public async Task ReceiveAndStartInspectionAsync(Guid returnId, Guid itemId, int quantity)
    {
        (await client.PostAsJsonAsync($"/api/returns/{returnId}/receive", new ReceiveCustomerReturnRequest
        {
            Items = [new ReceiveCustomerReturnItemRequest { ReturnItemId = itemId, ReceivedQuantity = quantity }]
        })).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/returns/{returnId}/inspection/start", null)).EnsureSuccessStatusCode();
    }

    public Task<HttpResponseMessage> CompleteInspectionAsync(Guid returnId, Guid itemId, int restocked, int quarantined) =>
        client.PostAsJsonAsync($"/api/returns/{returnId}/inspection/complete", new CompleteReturnInspectionRequest
        {
            Items =
            [
                new CompleteReturnInspectionItemRequest
                {
                    ReturnItemId = itemId,
                    Condition = quarantined > 0 ? ReturnItemCondition.Damaged : ReturnItemCondition.Good,
                    RestockedQuantity = restocked,
                    QuarantinedQuantity = quarantined,
                    DiscardedQuantity = 0,
                    RejectedQuantity = 0
                }
            ]
        });

    public async Task<ReturnResponse> GetReturnAsync(Guid returnId) =>
        (await client.GetFromJsonAsync<ReturnResponse>($"/api/returns/{returnId}", JsonOptions))!;

    public async Task<List<ReturnStockMovementResponse>> GetReturnMovementsAsync(Guid returnId) =>
        (await client.GetFromJsonAsync<List<ReturnStockMovementResponse>>($"/api/returns/{returnId}/stock-movements", JsonOptions))!;

    public async Task<List<ReturnableItemResponse>> GetEligibleItemsAsync(Guid orderId) =>
        (await client.GetFromJsonAsync<List<ReturnableItemResponse>>(
            $"/api/returns/customer/eligible-orders/{orderId}/items",
            JsonOptions))!;

    public async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId) =>
        (await client.GetFromJsonAsync<InventoryBalanceResponse>($"/api/inventory/balances/{productId}", JsonOptions))!;

    public async Task<PagedResult<FinancialTitleSummaryResponse>> GetTitlesAsync(string kind, Guid sourceId) =>
        (await client.GetFromJsonAsync<PagedResult<FinancialTitleSummaryResponse>>(
            $"/api/finance/{kind}?sourceId={sourceId}&page=1&pageSize=10",
            JsonOptions))!;

    public async Task<FinancialTitleResponse> GetTitleAsync(string kind, Guid id) =>
        (await client.GetFromJsonAsync<FinancialTitleResponse>($"/api/finance/{kind}/{id}", JsonOptions))!;

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}
