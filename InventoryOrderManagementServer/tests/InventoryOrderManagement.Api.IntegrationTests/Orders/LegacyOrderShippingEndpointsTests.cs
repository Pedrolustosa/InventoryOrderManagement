using System.Net;
using System.Net.Http.Json;
using InventoryOrderManagement.Api.IntegrationTests.Inventory;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Api.IntegrationTests.Orders;

/// <summary>
/// The legacy order ship/deliver endpoints are gone: shipping and delivery happen only through the shipment.
/// </summary>
public class LegacyOrderShippingEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;
    private readonly HttpClient _client;
    private readonly LowStockFlow _flow;

    public LegacyOrderShippingEndpointsTests(ProductsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _flow = new LowStockFlow(_client);
    }

    [Theory]
    [InlineData("ship")]
    [InlineData("deliver")]
    public async Task LegacyEndpoint_Returns410PointingToShipments_AndChangesNothing(string action)
    {
        var productId = await _flow.SeedProductAsync($"legacy-{action}", onHand: 10, minimum: 0);
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 2);
        var before = await _flow.GetBalanceAsync(productId);

        var response = await _client.PostAsync($"/api/orders/{orderId}/{action}", null);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(OrderTimelineEndpointsTests.JsonOptions);
        Assert.Equal(410, problem!.Status);
        Assert.Contains("/api/shipments", problem.Detail);
        Assert.Equal("/api/shipments", problem.Extensions["replacement"]?.ToString());

        Assert.Equal(OrderStatus.Confirmed, (await _flow.GetOrderAsync(orderId)).Status);
        var after = await _flow.GetBalanceAsync(productId);
        Assert.Equal(before.QuantityOnHand, after.QuantityOnHand);
        Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
    }

    [Fact]
    public async Task ShipmentFlow_ShipsAndDeliversTheOrder_IssuingStockAndCreatingTheReceivable()
    {
        var productId = await _flow.SeedProductAsync("legacy-full-flow", onHand: 10, minimum: 0);
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 3);
        Assert.Equal(3, (await _flow.GetBalanceAsync(productId)).ReservedQuantity);

        var created = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest { OrderId = orderId, CarrierName = "Acme" });
        created.EnsureSuccessStatusCode();
        var shipment = (await created.Content.ReadFromJsonAsync<ShipmentResponse>(OrderTimelineEndpointsTests.JsonOptions))!;

        await StepAsync(shipment.Id, "dispatch");
        Assert.Equal(OrderStatus.Shipped, (await _flow.GetOrderAsync(orderId)).Status);
        var issued = await _flow.GetBalanceAsync(productId);
        Assert.Equal(7, issued.QuantityOnHand);
        Assert.Equal(0, issued.ReservedQuantity);

        await StepAsync(shipment.Id, "in-transit");
        await StepAsync(shipment.Id, "deliver");

        var order = await _flow.GetOrderAsync(orderId);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.ShippedAtUtc);
        Assert.NotNull(order.DeliveredAtUtc);

        var timeline = await _client.GetFromJsonAsync<OrderTimelineDto>($"/api/orders/{orderId}/timeline", OrderTimelineEndpointsTests.JsonOptions);
        var types = timeline!.Entries.Select(entry => entry.EventType).ToList();
        Assert.Contains("shipment.dispatched", types);
        Assert.Contains("stock.issued", types);
        Assert.Contains("finance.receivable_created", types);
        Assert.Contains("shipment.delivered", types);
        Assert.Contains("order.delivered", types);
    }

    [Fact]
    public async Task HistoricalShippedOrderWithoutShipment_StillLoadsWithItsTimeline()
    {
        // Orders shipped/delivered by the removed endpoints have no shipment; they are kept as they are.
        var productId = await _flow.SeedProductAsync("legacy-history", onHand: 10, minimum: 0);
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 1);
        var shippedAt = DateTime.UtcNow.AddDays(-2);
        var deliveredAt = DateTime.UtcNow.AddDays(-1);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Orders SET Status = 'Delivered', ShippedAtUtc = {shippedAt}, DeliveredAtUtc = {deliveredAt} WHERE Id = {orderId}");
        }

        var order = await _flow.GetOrderAsync(orderId);
        Assert.Equal(OrderStatus.Delivered, order.Status);

        var response = await _client.GetAsync($"/api/orders/{orderId}/timeline");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var timeline = await response.Content.ReadFromJsonAsync<OrderTimelineDto>(OrderTimelineEndpointsTests.JsonOptions);
        Assert.Contains(timeline!.Entries, entry => entry.EventType == "order.shipped");
        Assert.Contains(timeline.Entries, entry => entry.EventType == "order.delivered");
        Assert.DoesNotContain(timeline.Entries, entry => entry.Module == OrderTimelineModule.Shipment);
    }

    private async Task StepAsync(Guid shipmentId, string step) =>
        (await _client.PostAsJsonAsync($"/api/shipments/{shipmentId}/{step}", new ShipmentNotesRequest())).EnsureSuccessStatusCode();
}
