using System.Net;
using System.Net.Http.Json;
using InventoryOrderManagement.Api.IntegrationTests.Inventory;
using InventoryOrderManagement.Api.IntegrationTests.Products;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.IntegrationTests.Orders;

public class OrderCancellationEndpointsTests : IClassFixture<ProductsApiFactory>
{
    private readonly HttpClient _client;
    private readonly LowStockFlow _flow;

    public OrderCancellationEndpointsTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
        _flow = new LowStockFlow(_client);
    }

    [Fact]
    public async Task CancellingAConfirmedOrder_ReleasesTheReservation_CancelsThePendingShipment_AndShowsInTheTimeline()
    {
        var productId = await _flow.SeedProductAsync("cancel-flow", onHand: 10, minimum: 2);
        var before = await _flow.GetBalanceAsync(productId);

        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 3);
        var reserved = await _flow.GetBalanceAsync(productId);
        Assert.Equal(before.ReservedQuantity + 3, reserved.ReservedQuantity);

        var shipmentResponse = await _client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest
        {
            OrderId = orderId,
            CarrierName = "Acme"
        });
        shipmentResponse.EnsureSuccessStatusCode();
        var shipment = (await shipmentResponse.Content.ReadFromJsonAsync<ShipmentResponse>(OrderTimelineEndpointsTests.JsonOptions))!;

        var cancel = await _client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new CancelOrderRequest { Reason = "Customer changed plans" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var after = await _flow.GetBalanceAsync(productId);
        Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
        Assert.Equal(before.QuantityOnHand, after.QuantityOnHand);
        Assert.True(after.AvailableQuantity > reserved.AvailableQuantity);
        Assert.Equal(before.AvailableQuantity, after.AvailableQuantity);

        var cancelledShipment = await _client.GetFromJsonAsync<ShipmentResponse>(
            $"/api/shipments/{shipment.Id}",
            OrderTimelineEndpointsTests.JsonOptions);
        Assert.Equal(ShipmentStatus.Cancelled, cancelledShipment!.Status);
        Assert.Equal(OrderStatus.Cancelled, (await _flow.GetOrderAsync(orderId)).Status);

        var timeline = await _client.GetFromJsonAsync<OrderTimelineDto>(
            $"/api/orders/{orderId}/timeline",
            OrderTimelineEndpointsTests.JsonOptions);
        var released = Assert.Single(timeline!.Entries, entry => entry.EventType == "stock.reservation_released");
        Assert.Equal(3, released.Summary.Quantity);
        Assert.Contains(timeline.Entries, entry => entry.EventType == "shipment.cancelled" && entry.Status == "Cancelled");
        Assert.Contains(timeline.Entries, entry => entry.EventType == "order.cancelled");

        // Cancelling again is rejected and releases nothing more.
        var again = await _client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new CancelOrderRequest { Reason = "Twice" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(after.ReservedQuantity, (await _flow.GetBalanceAsync(productId)).ReservedQuantity);
    }

    [Fact]
    public async Task CancellingADispatchedOrder_Returns409_AndChangesNothing()
    {
        var productId = await _flow.SeedProductAsync("cancel-dispatched", onHand: 10, minimum: 0);
        var orderId = await _flow.CreateConfirmedOrderAsync(productId, quantity: 2);
        await _flow.CreateAndDispatchShipmentAsync(orderId);
        var before = await _flow.GetBalanceAsync(productId);

        var response = await _client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new CancelOrderRequest { Reason = "Too late" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(OrderTimelineEndpointsTests.JsonOptions);
        Assert.Equal(409, problem!.Status);

        var after = await _flow.GetBalanceAsync(productId);
        Assert.Equal(before.QuantityOnHand, after.QuantityOnHand);
        Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
        Assert.Equal(OrderStatus.Shipped, (await _flow.GetOrderAsync(orderId)).Status);
    }
}
