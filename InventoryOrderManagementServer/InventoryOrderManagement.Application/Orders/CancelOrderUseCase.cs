using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Orders;

public sealed class CancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderUseCase(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
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

            if (order.Status == OrderStatus.Confirmed)
            {
                foreach (var item in order.Items.OrderBy(line => line.ProductId))
                {
                    var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, ct)
                        ?? throw new InventoryBalanceNotFoundException(item.ProductId);

                    var previousQuantity = balance.QuantityOnHand;
                    balance.Receive(item.Quantity);

                    var movement = StockMovement.CreateForOrder(
                        item.ProductId,
                        order.Id,
                        StockMovementType.OrderCancellationReturn,
                        item.Quantity,
                        previousQuantity,
                        balance.QuantityOnHand,
                        $"Stock returned for cancelled order {order.OrderNumber}.");

                    await _inventoryRepository.AddMovementAsync(movement, ct);
                }
            }

            order.Cancel(request.Reason);
            response = OrderResponse.FromOrder(order);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Order was not cancelled.");
    }
}
