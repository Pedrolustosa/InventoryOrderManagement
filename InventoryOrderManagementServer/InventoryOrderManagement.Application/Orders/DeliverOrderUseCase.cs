using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;

namespace InventoryOrderManagement.Application.Orders;

public sealed class DeliverOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeliverOrderUseCase(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        OrderResponse? response = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetTrackedByIdAsync(orderId, ct)
                ?? throw new OrderNotFoundException(orderId);

            order.Deliver();
            response = OrderResponse.FromOrder(order);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Order was not delivered.");
    }
}
