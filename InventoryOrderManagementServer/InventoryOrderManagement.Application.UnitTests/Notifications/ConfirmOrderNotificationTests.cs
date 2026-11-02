using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class ConfirmOrderNotificationTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<ConfirmOrderUseCase> _logger = Substitute.For<ILogger<ConfirmOrderUseCase>>();

    public ConfirmOrderNotificationTests()
    {
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task ExecuteAsync_NotifiesOrderConfirmed()
    {
        var product = Product.Create("SKU-1", "Widget", null, 10m, Guid.NewGuid());
        var customer = Customer.Create(CustomerType.Individual, "Jane Doe", null, null, null, null);
        var order = Order.Create(
            "ORD-100",
            customer.Id,
            customer.Name,
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(product.Id, product.Sku, product.Name, 2, 10m)]);

        var balance = InventoryBalance.Create(product.Id);
        balance.Receive(20);
        balance.SetMinimumStockLevel(5);

        _orders.GetTrackedByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _inventory.GetTrackedByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(balance);

        var useCase = new ConfirmOrderUseCase(
            _orders,
            _customers,
            _products,
            _inventory,
            _unitOfWork,
            _dispatcher,
            _logger);

        var response = await useCase.ExecuteAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, response.Status);
        await _dispatcher.Received(1).NotifyOrderConfirmedAsync(
            order.Id,
            order.OrderNumber,
            order.CustomerName,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
