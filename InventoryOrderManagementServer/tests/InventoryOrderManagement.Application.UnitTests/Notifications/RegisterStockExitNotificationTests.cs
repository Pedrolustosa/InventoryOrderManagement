using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class RegisterStockExitNotificationTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IInventoryRepository _inventory = Substitute.For<IInventoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<RegisterStockExitUseCase> _logger = Substitute.For<ILogger<RegisterStockExitUseCase>>();

    public RegisterStockExitNotificationTests()
    {
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCrossingMinimum_NotifiesAfterCommit()
    {
        var product = Product.Create("SKU-1", "Widget", null, 10m, Guid.NewGuid());
        var balance = InventoryBalance.Create(product.Id);
        balance.Receive(15);
        balance.SetMinimumStockLevel(5);

        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _inventory.GetTrackedByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(balance);

        var useCase = new RegisterStockExitUseCase(_products, _inventory, _unitOfWork, _dispatcher, _logger);
        await useCase.ExecuteAsync(new RegisterStockExitRequest
        {
            ProductId = product.Id,
            Quantity = 11,
            Reason = "Sale"
        });

        await _dispatcher.Received(1).NotifyLowStockAsync(
            product.Id,
            product.Name,
            product.Sku,
            15,
            4,
            5,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDispatcherThrows_StillReturnsBusinessResult()
    {
        var product = Product.Create("SKU-1", "Widget", null, 10m, Guid.NewGuid());
        var balance = InventoryBalance.Create(product.Id);
        balance.Receive(15);
        balance.SetMinimumStockLevel(5);

        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _inventory.GetTrackedByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(balance);
        _dispatcher
            .When(d => d.NotifyLowStockAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("notify failed"));

        var useCase = new RegisterStockExitUseCase(_products, _inventory, _unitOfWork, _dispatcher, _logger);
        var response = await useCase.ExecuteAsync(new RegisterStockExitRequest
        {
            ProductId = product.Id,
            Quantity = 11,
            Reason = "Sale"
        });

        Assert.Equal(4, response.NewQuantity);
        Assert.Contains(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }
}
