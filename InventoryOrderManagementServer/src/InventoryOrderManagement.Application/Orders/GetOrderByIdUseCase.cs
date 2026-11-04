using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;

namespace InventoryOrderManagement.Application.Orders;

public sealed class GetOrderByIdUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException(orderId);

        return OrderResponse.FromOrder(order);
    }
}
