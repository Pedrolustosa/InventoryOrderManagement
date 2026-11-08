using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Orders;

public sealed class OrderConfirmedHandler : ITransactionalDomainEventHandler<OrderConfirmedDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public OrderConfirmedHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task HandleAsync(
        OrderConfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var order = await _orderRepository.GetTrackedByIdAsync(domainEvent.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(domainEvent.OrderId);

        var lines = domainEvent.Items
            .OrderBy(item => item.ProductId)
            .ToList();

        var prepared = new List<(OrderConfirmedItem Item, InventoryBalance Balance, string ProductName)>(lines.Count);

        foreach (var item in lines)
        {
            var orderItem = order.Items.FirstOrDefault(line => line.ProductId == item.ProductId);
            var productName = orderItem?.ProductName ?? item.ProductId.ToString();

            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, cancellationToken);
            if (balance is null || item.Quantity > balance.AvailableQuantity)
            {
                throw new OrderInsufficientStockException(
                    productName,
                    balance?.AvailableQuantity ?? 0,
                    item.Quantity);
            }

            prepared.Add((item, balance, productName));
        }

        foreach (var (item, balance, _) in prepared)
        {
            var previousReserved = balance.ReservedQuantity;

            try
            {
                balance.Reserve(item.Quantity);
            }
            catch (InsufficientStockException exception)
            {
                throw new OrderInsufficientStockException(
                    order.Items.First(line => line.ProductId == item.ProductId).ProductName,
                    exception.AvailableQuantity,
                    exception.RequestedQuantity);
            }

            var movement = StockMovement.CreateForOrder(
                item.ProductId,
                order.Id,
                StockMovementType.OrderReservation,
                item.Quantity,
                previousReserved,
                balance.ReservedQuantity,
                $"Stock reserved for order {order.OrderNumber}.");

            await _inventoryRepository.AddMovementAsync(movement, cancellationToken);
        }
    }
}
