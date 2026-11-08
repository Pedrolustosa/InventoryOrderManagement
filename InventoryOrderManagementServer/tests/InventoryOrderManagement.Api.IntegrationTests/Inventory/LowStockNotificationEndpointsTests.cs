using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryOrderManagement.Api.IntegrationTests.Inventory;

public class LowStockNotificationEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;
    private readonly LowStockFlow _flow;

    public LowStockNotificationEndpointsTests(ProductsApiFactory factory)
    {
        _factory = factory;
        _flow = new LowStockFlow(factory.CreateClient());
    }

    [Fact]
    public async Task ConfirmingAnOrderBelowTheMinimum_NotifiesInventoryOperators_AndDispatchDoesNotRepeat()
    {
        var operatorId = await CreateUserAsync(_factory.Services, SystemRoles.InventoryOperator, "low-op");
        var viewerId = await CreateUserAsync(_factory.Services, SystemRoles.Viewer, "low-viewer");
        var productId = await _flow.SeedProductAsync("low-stock-flow", onHand: 6, minimum: 5);

        // Reserving 2 of 6 leaves 4 available, below the minimum of 5.
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 2);

        var operatorAlert = Assert.Single(await LowStockAlertsAsync(_factory.Services, operatorId, productId));
        Assert.False(operatorAlert.IsRead);
        Assert.Equal(NotificationSeverity.Warning, operatorAlert.Severity);
        Assert.Equal($"/inventory?productId={productId}", operatorAlert.TargetRoute);
        Assert.Contains("Available: 4", operatorAlert.Message);
        Assert.Contains("Minimum: 5", operatorAlert.Message);
        Assert.Empty(await LowStockAlertsAsync(_factory.Services, viewerId, productId));

        // Dispatch consumes the reservation: available stays 4, so no new crossing and no new alert.
        await _flow.CreateAndDispatchShipmentAsync(orderId);
        var balance = await _flow.GetBalanceAsync(productId);
        Assert.Equal(4, balance.QuantityOnHand);
        Assert.Equal(4, balance.AvailableQuantity);
        Assert.True(balance.IsLowStock);
        Assert.Single(await LowStockAlertsAsync(_factory.Services, operatorId, productId));
    }

    internal static async Task<Guid> CreateUserAsync(IServiceProvider services, string role, string prefix)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{prefix}-{Guid.NewGuid():N}@localhost";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = prefix,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user.Id;
    }

    internal static async Task<List<Domain.Entities.UserNotification>> LowStockAlertsAsync(
        IServiceProvider services,
        Guid userId,
        Guid productId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await dbContext.UserNotifications.AsNoTracking()
            .Where(notification => notification.RecipientUserId == userId
                && notification.Type == NotificationType.LowStockDetected
                && notification.RelatedEntityId == productId)
            .ToListAsync();
    }
}

public class LowStockNotificationFailureTests : IClassFixture<FailingNotificationRecipientsApiFactory>
{
    private readonly FailingNotificationRecipientsApiFactory _factory;
    private readonly LowStockFlow _flow;

    public LowStockNotificationFailureTests(FailingNotificationRecipientsApiFactory factory)
    {
        _factory = factory;
        _flow = new LowStockFlow(factory.CreateClient());
    }

    [Fact]
    public async Task WhenNotifyingFails_TheStockOperationIsStillPersisted()
    {
        var operatorId = await LowStockNotificationEndpointsTests.CreateUserAsync(
            _factory.Services,
            SystemRoles.InventoryOperator,
            "low-fail-op");
        var productId = await _flow.SeedProductAsync("low-stock-fail", onHand: 6, minimum: 5);

        // CreateConfirmedOrderAsync asserts the confirmation returned 200 despite the failing notification.
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 2);

        var balance = await _flow.GetBalanceAsync(productId);
        Assert.Equal(2, balance.ReservedQuantity);
        Assert.Equal(4, balance.AvailableQuantity);
        var order = await _flow.GetOrderAsync(orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Empty(await LowStockNotificationEndpointsTests.LowStockAlertsAsync(_factory.Services, operatorId, productId));
    }
}

/// <summary>Notification recipients cannot be resolved, so every post-commit notification fails.</summary>
public sealed class FailingNotificationRecipientsApiFactory : ProductsApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificationRecipientResolver>();
            services.AddScoped<INotificationRecipientResolver, ThrowingRecipientResolver>();
        });
    }

    private sealed class ThrowingRecipientResolver : INotificationRecipientResolver
    {
        public Task<IReadOnlyCollection<Guid>> GetActiveUserIdsByPermissionAsync(
            string permission,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated notification failure.");
    }
}

internal sealed class LowStockFlow(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Guid> SeedProductAsync(string sku, int onHand, int minimum)
    {
        var supplier = await PostAsync<SupplierResponse>("/api/suppliers", new CreateSupplierRequest { Name = $"Supplier {sku}" });
        var product = await PostAsync<ProductResponse>("/api/products", new CreateProductRequest
        {
            Sku = sku,
            Name = $"Product {sku}",
            CurrentPrice = 10m,
            CategoryId = CategoryConfiguration.UncategorizedCategoryId
        });

        (await client.PostAsJsonAsync("/api/inventory/entries", new ReceiveStockRequest
        {
            ProductId = product.Id,
            SupplierId = supplier.Id,
            Quantity = onHand
        })).EnsureSuccessStatusCode();

        var minimumResponse = await client.PutAsJsonAsync(
            $"/api/inventory/balances/{product.Id}/minimum-stock",
            new UpdateMinimumStockLevelRequest { MinimumStockLevel = minimum });
        Assert.Equal(HttpStatusCode.NoContent, minimumResponse.StatusCode);
        return product.Id;
    }

    public async Task<Guid> CreateConfirmedOrderAsync(Guid productId, int quantity)
    {
        var customer = await PostAsync<CustomerResponse>("/api/customers", new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = $"Customer {Guid.NewGuid():N}"
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
            Items = [new OrderItemRequest { ProductId = productId, Quantity = quantity }]
        });

        var confirm = await client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        return order.Id;
    }

    public async Task CreateAndDispatchShipmentAsync(Guid orderId)
    {
        var shipment = await PostAsync<ShipmentResponse>("/api/shipments", new CreateShipmentRequest
        {
            OrderId = orderId,
            CarrierName = "Acme"
        });
        var dispatch = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/dispatch", new ShipmentNotesRequest());
        Assert.Equal(HttpStatusCode.OK, dispatch.StatusCode);
    }

    public async Task<InventoryBalanceResponse> GetBalanceAsync(Guid productId) =>
        (await client.GetFromJsonAsync<InventoryBalanceResponse>($"/api/inventory/balances/{productId}", JsonOptions))!;

    public async Task<OrderResponse> GetOrderAsync(Guid orderId) =>
        (await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", JsonOptions))!;

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}
