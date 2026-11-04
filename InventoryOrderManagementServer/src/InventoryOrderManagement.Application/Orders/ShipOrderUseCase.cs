using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Orders;

public sealed class ShipOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShipOrderUseCase(
        IOrderRepository orderRepository,
        IShipmentRepository shipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        OrderResponse? response = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            if (await _shipmentRepository.ExistsForOrderAsync(orderId, ct))
            {
                throw new ShipmentConflictException(
                    "This order has a shipment. Use the shipment dispatch workflow instead of the order ship endpoint.");
            }

            var order = await _orderRepository.GetTrackedByIdAsync(orderId, ct)
                ?? throw new OrderNotFoundException(orderId);

            order.Ship();
            response = OrderResponse.FromOrder(order);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Order was not shipped.");
    }
}
