using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Application.Shipments;

/// <summary>
/// Runs inside the dispatch transaction: issues the shipped stock and creates the sales-order receivable.
/// Any exception rolls back the stock, the receivable and the shipment/order status changes together.
/// </summary>
/// <remarks>
/// Stock rules, decided per order (never from the product-wide reserved total):
/// <list type="number">
///   <item>The shipment already has an OrderExit movement: stock was issued before, so nothing is issued again.</item>
///   <item>The order has an OrderExit not linked to a shipment: it was confirmed by the pre-reservation flow,
///   which issued QuantityOnHand at confirmation, so nothing is issued again.</item>
///   <item>The order's net reservation covers the item: <see cref="InventoryBalance.ConsumeReservation"/>
///   lowers QuantityOnHand and ReservedQuantity together.</item>
///   <item>The order has no reservation for the item (legacy data): QuantityOnHand is issued directly,
///   provided the AvailableQuantity (on hand minus other orders' reservations) covers it.</item>
///   <item>A partial reservation is inconsistent data and rejects the dispatch.</item>
/// </list>
/// Receivable rule: one receivable per sales order (enforced by a unique index), for the order subtotal
/// (shipments always cover the whole order), due after Finance:DefaultReceivableDays because neither
/// orders nor customers carry payment terms.
/// </remarks>
public sealed class ShipmentDispatchedHandler : ITransactionalDomainEventHandler<ShipmentDispatchedDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IFinancialTitleRepository _financialTitleRepository;
    private readonly TimeProvider _timeProvider;
    private readonly FinanceOptions _financeOptions;

    public ShipmentDispatchedHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IFinancialTitleRepository financialTitleRepository,
        TimeProvider timeProvider,
        IOptions<FinanceOptions> financeOptions)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _financialTitleRepository = financialTitleRepository;
        _timeProvider = timeProvider;
        _financeOptions = financeOptions.Value;
    }

    public async Task HandleAsync(
        ShipmentDispatchedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var order = await _orderRepository.GetTrackedByIdAsync(domainEvent.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(domainEvent.OrderId);

        if (await ShouldIssueStockAsync(domainEvent, cancellationToken))
        {
            await IssueStockAsync(order, domainEvent, cancellationToken);
        }

        if (!await _financialTitleRepository.ExistsReceivableForSalesOrderAsync(order.Id, cancellationToken))
        {
            await CreateReceivableAsync(order, cancellationToken);
        }
    }

    private async Task<bool> ShouldIssueStockAsync(
        ShipmentDispatchedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        if (await _inventoryRepository.ExistsShipmentExitAsync(domainEvent.ShipmentId, cancellationToken))
        {
            return false;
        }

        return !await _inventoryRepository.ExistsLegacyOrderExitAsync(domainEvent.OrderId, cancellationToken);
    }

    private async Task IssueStockAsync(
        Order order,
        ShipmentDispatchedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var reservedByProduct = await _inventoryRepository.GetNetReservedByProductAsync(order.Id, cancellationToken);

        var lines = domainEvent.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new ShipmentDispatchedItem(group.Key, group.Sum(item => item.Quantity)))
            .OrderBy(item => item.ProductId)
            .ToList();

        foreach (var item in lines)
        {
            var productName = order.Items.FirstOrDefault(line => line.ProductId == item.ProductId)?.ProductName
                ?? item.ProductId.ToString();

            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, cancellationToken)
                ?? throw new InventoryBalanceNotFoundException(item.ProductId);

            var previousOnHand = balance.QuantityOnHand;
            var reserved = reservedByProduct.GetValueOrDefault(item.ProductId);

            try
            {
                if (reserved >= item.Quantity)
                {
                    balance.ConsumeReservation(item.Quantity);
                }
                else if (reserved == 0)
                {
                    if (item.Quantity > balance.AvailableQuantity)
                    {
                        throw new OrderInsufficientStockException(
                            productName,
                            balance.AvailableQuantity,
                            item.Quantity);
                    }

                    balance.Issue(item.Quantity);
                }
                else
                {
                    throw new ShipmentConflictException(
                        $"Order {order.OrderNumber} reserved {reserved} units of '{productName}' but the shipment requires {item.Quantity}.");
                }
            }
            catch (InsufficientStockException exception)
            {
                throw new OrderInsufficientStockException(
                    productName,
                    exception.AvailableQuantity,
                    exception.RequestedQuantity);
            }
            catch (DomainException exception)
            {
                throw new ShipmentConflictException(exception.Message);
            }

            var movement = StockMovement.CreateForShipment(
                item.ProductId,
                order.Id,
                domainEvent.ShipmentId,
                item.Quantity,
                previousOnHand,
                balance.QuantityOnHand,
                $"Stock issued for dispatched shipment of order {order.OrderNumber}.");

            await _inventoryRepository.AddMovementAsync(movement, cancellationToken);
        }
    }

    private async Task CreateReceivableAsync(Order order, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var issueDate = DateOnly.FromDateTime(now);
        var dueDate = issueDate.AddDays(_financeOptions.DefaultReceivableDays);
        var titleNumber = await FinancialTitleNumbers.NextAsync(_financialTitleRepository, now, cancellationToken);
        var documentNumber = $"SO-{order.OrderNumber}";

        var title = FinancialTitle.Create(
            titleNumber,
            FinancialTitleType.Receivable,
            $"Receivable for sales order {order.OrderNumber}",
            FinancialCounterpartyType.Customer,
            order.CustomerId,
            order.CustomerName,
            FinancialSourceType.SalesOrder,
            order.Id,
            order.OrderNumber,
            documentNumber,
            issueDate,
            order.Subtotal,
            notes: null,
            createdByUserId: null,
            [(1, dueDate, order.Subtotal)],
            now);

        if (await _financialTitleRepository.DocumentExistsAsync(title, cancellationToken))
        {
            throw new DuplicateFinancialDocumentException(documentNumber);
        }

        await _financialTitleRepository.AddAsync(title, cancellationToken);
    }
}
