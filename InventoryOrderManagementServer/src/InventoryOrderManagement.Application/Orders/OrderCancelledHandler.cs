using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Runs inside the cancellation transaction and undoes what confirming the order committed. Any failure rolls
/// back the stock changes, the order status and the shipment status together.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>Draft orders committed nothing: no stock change.</item>
///   <item>Confirmed orders: releases exactly the net reservation recorded for the order (reservations minus
///   releases, per product), not the current item quantities.</item>
///   <item>Legacy orders confirmed before reservations existed issued on-hand stock at confirmation
///   (OrderExit without a shipment): that stock is returned (OrderCancellationReturn), net of earlier returns.
///   An order with neither is only logged.</item>
///   <item>A shipment still in Preparing is cancelled with the order.</item>
/// </list>
/// Everything is computed from the order's movements, so handling the event again releases or returns nothing.
/// </remarks>
public sealed class OrderCancelledHandler : ITransactionalDomainEventHandler<OrderCancelledDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<OrderCancelledHandler> _logger;

    public OrderCancelledHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IShipmentRepository shipmentRepository,
        ICurrentUser currentUser,
        ILogger<OrderCancelledHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _shipmentRepository = shipmentRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(
        OrderCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent.PreviousStatus != OrderStatus.Confirmed)
        {
            return;
        }

        var order = await _orderRepository.GetTrackedByIdAsync(domainEvent.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(domainEvent.OrderId);

        var reserved = await _inventoryRepository.GetNetReservedByProductAsync(order.Id, cancellationToken);
        if (reserved.Values.Any(quantity => quantity > 0))
        {
            await ReleaseReservationsAsync(order, reserved, cancellationToken);
        }
        else
        {
            await ReturnLegacyIssuesAsync(order, cancellationToken);
        }

        await CancelPendingShipmentAsync(order, cancellationToken);
    }

    private async Task ReleaseReservationsAsync(
        Order order,
        IReadOnlyDictionary<Guid, int> reserved,
        CancellationToken cancellationToken)
    {
        foreach (var (productId, quantity) in reserved.Where(pair => pair.Value > 0).OrderBy(pair => pair.Key))
        {
            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(productId, cancellationToken)
                ?? throw new InventoryBalanceNotFoundException(productId);

            var previousReserved = balance.ReservedQuantity;
            balance.ReleaseReservation(quantity);

            await _inventoryRepository.AddMovementAsync(
                StockMovement.CreateForOrder(
                    productId,
                    order.Id,
                    StockMovementType.OrderReservationRelease,
                    quantity,
                    previousReserved,
                    balance.ReservedQuantity,
                    $"Stock reservation released for cancelled order {order.OrderNumber}."),
                cancellationToken);
        }
    }

    private async Task ReturnLegacyIssuesAsync(Order order, CancellationToken cancellationToken)
    {
        var issued = await _inventoryRepository.GetNetLegacyExitByProductAsync(order.Id, cancellationToken);
        if (!issued.Values.Any(quantity => quantity > 0))
        {
            _logger.LogWarning(
                "Cancelled order {OrderNumber} ({OrderId}) has no reservation or legacy stock issue to undo.",
                order.OrderNumber,
                order.Id);
            return;
        }

        _logger.LogInformation(
            "Cancelled legacy order {OrderNumber} ({OrderId}) was confirmed without a reservation; returning the stock issued at confirmation.",
            order.OrderNumber,
            order.Id);

        foreach (var (productId, quantity) in issued.Where(pair => pair.Value > 0).OrderBy(pair => pair.Key))
        {
            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(productId, cancellationToken)
                ?? throw new InventoryBalanceNotFoundException(productId);

            var previousOnHand = balance.QuantityOnHand;
            balance.Receive(quantity);

            await _inventoryRepository.AddMovementAsync(
                StockMovement.CreateForOrder(
                    productId,
                    order.Id,
                    StockMovementType.OrderCancellationReturn,
                    quantity,
                    previousOnHand,
                    balance.QuantityOnHand,
                    $"Stock returned for cancelled legacy order {order.OrderNumber}."),
                cancellationToken);
        }
    }

    private async Task CancelPendingShipmentAsync(Order order, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetTrackedByOrderIdAsync(order.Id, cancellationToken);
        if (shipment?.Status == ShipmentStatus.Preparing)
        {
            shipment.Cancel($"Order {order.OrderNumber} was cancelled.", _currentUser.UserId);
        }
    }
}
