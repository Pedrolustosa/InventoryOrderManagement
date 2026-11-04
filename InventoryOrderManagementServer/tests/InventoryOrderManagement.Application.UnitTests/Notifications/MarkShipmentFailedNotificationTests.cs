using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Shipments;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class MarkShipmentFailedNotificationTests
{
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISecurityAuditWriter _audit = Substitute.For<ISecurityAuditWriter>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<MarkShipmentFailedUseCase> _logger = Substitute.For<ILogger<MarkShipmentFailedUseCase>>();

    public MarkShipmentFailedNotificationTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task ExecuteAsync_NotifiesShipmentFailed()
    {
        var order = Order.Create(
            "ORD-200",
            Guid.NewGuid(),
            "Jane Doe",
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(Guid.NewGuid(), "SKU", "Name", 1, 10m)]);
        order.Confirm();
        order.Ship();

        var shipment = Shipment.Create(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.CustomerName,
            "Acme",
            "TRK-1",
            null,
            Guid.NewGuid());
        shipment.Dispatch(null, null);

        _shipments.GetTrackedByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var useCase = new MarkShipmentFailedUseCase(
            _shipments,
            _orders,
            _unitOfWork,
            _audit,
            _currentUser,
            _dispatcher,
            _logger);

        await useCase.ExecuteAsync(shipment.Id, new FailShipmentRequest
        {
            FailureReason = "Customer absent",
            Notes = "Attempt 1"
        });

        await _dispatcher.Received(1).NotifyShipmentFailedAsync(
            shipment.Id,
            order.OrderNumber,
            Arg.Any<Guid>(),
            "Customer absent",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
