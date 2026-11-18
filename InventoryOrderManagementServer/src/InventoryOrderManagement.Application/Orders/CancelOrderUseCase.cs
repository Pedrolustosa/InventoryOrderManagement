using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;

namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Cancels a Draft or Confirmed order. Releasing the reservation (or returning legacy stock) and cancelling a
/// pending shipment happen in <see cref="OrderCancelledHandler"/>, inside this same transaction.
/// </summary>
public sealed class CancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderUseCase(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> ExecuteAsync(
        Guid orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OrderResponse? response = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetTrackedByIdAsync(orderId, ct)
                ?? throw new OrderNotFoundException(orderId);

            order.Cancel(request.Reason);
            response = OrderResponse.FromOrder(order);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Order was not cancelled.");
    }
}
