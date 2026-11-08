using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Events;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;
using Microsoft.Extensions.Options;

namespace InventoryOrderManagement.Application.Returns;

/// <summary>
/// Runs inside the transaction that completes a return (customer/exchange inspection or supplier dispatch)
/// and reverses stock and finance. Any exception rolls back the stock, the financial changes and the return
/// status together.
/// </summary>
/// <remarks>
/// <para><b>Stock.</b> Customer returns and exchanges: restocked units are received into on-hand
/// (<c>CustomerReturnEntry</c>); quarantined and discarded units are recorded only
/// (<c>CustomerReturnQuarantine</c> / <c>CustomerReturnLoss</c>) and never reach the available balance;
/// rejected units go back to the customer. Exchange replacements leave stock (<c>ExchangeReplacementExit</c>)
/// after validating the available quantity. Supplier returns issue the dispatched units
/// (<c>SupplierReturnExit</c>) after validating the available quantity.</para>
/// <para><b>Finance</b> (proportional: accepted or dispatched quantity × unit value snapshot):
/// the original title (sales-order receivable for customers, purchase-order payable for suppliers) is
/// abated through <see cref="FinancialTitle.ApplyReturnCredit"/>, which cancels it when nothing was settled
/// and the whole balance is reversed. The part already paid, or the whole amount when no open original
/// title exists, becomes a new title: a payable refund/credit to the customer or a receivable from the
/// supplier. Exchanges settle only the price difference: none when equal, a receivable when the
/// replacements cost more, the customer refund rule when they cost less. Financial treatment <c>None</c>
/// skips finance for customer and supplier returns.</para>
/// <para><b>Idempotency.</b> Stock runs only when the return has no stock movement yet (backed by a unique
/// index); finance runs only while the financial status is <c>Pending</c> (backed by a unique source index
/// on generated titles and unique settlement keys).</para>
/// </remarks>
public sealed class ReturnCompletedHandler : ITransactionalDomainEventHandler<ReturnCompletedDomainEvent>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IFinancialTitleRepository _financialTitleRepository;
    private readonly TimeProvider _timeProvider;
    private readonly FinanceOptions _financeOptions;

    public ReturnCompletedHandler(
        IReturnRequestRepository returnRepository,
        IOrderRepository orderRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IInventoryRepository inventoryRepository,
        IFinancialTitleRepository financialTitleRepository,
        TimeProvider timeProvider,
        IOptions<FinanceOptions> financeOptions)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _inventoryRepository = inventoryRepository;
        _financialTitleRepository = financialTitleRepository;
        _timeProvider = timeProvider;
        _financeOptions = financeOptions.Value;
    }

    public async Task HandleAsync(
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var entity = await _returnRepository.GetTrackedByIdAsync(domainEvent.ReturnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(domainEvent.ReturnRequestId);

        if (!await _inventoryRepository.ExistsReturnMovementAsync(entity.Id, cancellationToken))
        {
            if (entity.Type == ReturnType.SupplierReturn)
            {
                await RegisterSupplierReturnAsync(entity, domainEvent, cancellationToken);
                await IssueSupplierReturnAsync(entity, domainEvent, cancellationToken);
            }
            else
            {
                await RegisterCustomerReturnAsync(entity, domainEvent, cancellationToken);
                await ReceiveCustomerReturnAsync(entity, domainEvent, cancellationToken);
                await IssueReplacementsAsync(entity, domainEvent, cancellationToken);
            }
        }

        if (entity.FinancialStatus == ReturnFinancialStatus.Pending)
        {
            await ReverseFinanceAsync(entity, domainEvent, cancellationToken);
        }
    }

    private async Task RegisterCustomerReturnAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var orderId = domainEvent.OrderId ?? throw new ReturnConflictException("The return has no sales order.");
        var order = await _orderRepository.GetTrackedByIdAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException(orderId);

        var lines = domainEvent.Items
            .Select(item => (ItemFor(entity, item).OrderItemId!.Value, item.Quantity))
            .ToList();

        // Validates that the returned quantity never exceeds the sold quantity minus earlier returns.
        order.RegisterCustomerReturn(lines);
    }

    private async Task RegisterSupplierReturnAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var purchaseOrderId = domainEvent.PurchaseOrderId
            ?? throw new ReturnConflictException("The return has no purchase order.");
        var purchaseOrder = await _purchaseOrderRepository.GetTrackedByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        var lines = domainEvent.Items
            .Select(item => (ItemFor(entity, item).PurchaseOrderItemId!.Value, item.Quantity))
            .ToList();

        // Validates that the returned quantity never exceeds the received quantity minus earlier returns.
        purchaseOrder.RegisterSupplierReturn(lines);
    }

    private async Task ReceiveCustomerReturnAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var orderId = domainEvent.OrderId!.Value;

        foreach (var item in domainEvent.Items.OrderBy(line => line.ProductId))
        {
            var balance = await BalanceAsync(item.ProductId, cancellationToken);
            var reason = $"Customer return {entity.ReturnNumber}";

            if (item.RestockedQuantity > 0)
            {
                var previous = balance.QuantityOnHand;
                balance.Receive(item.RestockedQuantity);
                await _inventoryRepository.AddMovementAsync(
                    StockMovement.CreateForCustomerReturn(
                        item.ProductId,
                        orderId,
                        entity.Id,
                        item.ReturnItemId,
                        item.RestockedQuantity,
                        previous,
                        balance.QuantityOnHand,
                        reason),
                    cancellationToken);
            }

            if (item.QuarantinedQuantity > 0)
            {
                await _inventoryRepository.AddMovementAsync(
                    StockMovement.CreateForCustomerReturn(
                        item.ProductId,
                        orderId,
                        entity.Id,
                        item.ReturnItemId,
                        item.QuarantinedQuantity,
                        balance.QuantityOnHand,
                        balance.QuantityOnHand,
                        $"{reason}: quarantined ({item.Condition})",
                        StockMovementType.CustomerReturnQuarantine),
                    cancellationToken);
            }

            if (item.DiscardedQuantity > 0)
            {
                await _inventoryRepository.AddMovementAsync(
                    StockMovement.CreateForCustomerReturn(
                        item.ProductId,
                        orderId,
                        entity.Id,
                        item.ReturnItemId,
                        item.DiscardedQuantity,
                        balance.QuantityOnHand,
                        balance.QuantityOnHand,
                        $"{reason}: discarded as loss ({item.Condition})",
                        StockMovementType.CustomerReturnLoss),
                    cancellationToken);
            }
        }
    }

    private async Task IssueReplacementsAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        foreach (var replacement in domainEvent.Replacements.OrderBy(line => line.ProductId))
        {
            var productName = entity.ReplacementItems
                .FirstOrDefault(item => item.Id == replacement.ReplacementItemId)?.ProductName
                ?? replacement.ProductId.ToString();
            var balance = await BalanceAsync(replacement.ProductId, cancellationToken);
            EnsureAvailable(balance, replacement.Quantity, productName);

            var previous = balance.QuantityOnHand;
            balance.Issue(replacement.Quantity);
            await _inventoryRepository.AddMovementAsync(
                StockMovement.CreateForExchangeReplacement(
                    replacement.ProductId,
                    domainEvent.OrderId!.Value,
                    entity.Id,
                    replacement.Quantity,
                    previous,
                    balance.QuantityOnHand,
                    $"Exchange replacement {entity.ReturnNumber}"),
                cancellationToken);
        }
    }

    private async Task IssueSupplierReturnAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        foreach (var item in domainEvent.Items.OrderBy(line => line.ProductId))
        {
            var returnItem = ItemFor(entity, item);
            var balance = await BalanceAsync(item.ProductId, cancellationToken);
            EnsureAvailable(balance, item.Quantity, returnItem.ProductName);

            var previous = balance.QuantityOnHand;
            balance.Issue(item.Quantity);
            await _inventoryRepository.AddMovementAsync(
                StockMovement.CreateForSupplierReturn(
                    item.ProductId,
                    entity.SupplierId!.Value,
                    domainEvent.PurchaseOrderId!.Value,
                    entity.Id,
                    item.ReturnItemId,
                    item.Quantity,
                    previous,
                    balance.QuantityOnHand,
                    $"Supplier return {entity.ReturnNumber}"),
                cancellationToken);
        }
    }

    private async Task ReverseFinanceAsync(
        ReturnRequest entity,
        ReturnCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var returnedValue = Money.Round(domainEvent.Items.Sum(item => item.Quantity * item.UnitValue));

        switch (entity.Type)
        {
            case ReturnType.SupplierReturn:
                await ReverseSupplierAsync(entity, returnedValue, cancellationToken);
                break;
            case ReturnType.Exchange:
                var difference = Money.Round(
                    domainEvent.Replacements.Sum(item => item.Quantity * item.UnitPrice) - returnedValue);
                if (difference > 0)
                {
                    var receivable = await CreateReturnTitleAsync(
                        entity,
                        FinancialTitleType.Receivable,
                        FinancialCounterpartyType.Customer,
                        entity.CustomerId!.Value,
                        entity.CustomerName!,
                        $"Exchange difference for return {entity.ReturnNumber}",
                        difference,
                        cancellationToken);
                    entity.MarkFinanciallyProcessed(null, 0m, receivable.Id);
                }
                else if (difference < 0)
                {
                    await ReverseCustomerAsync(entity, -difference, cancellationToken);
                }
                else
                {
                    entity.MarkFinanciallyProcessed(null, 0m, null);
                }

                break;
            default:
                await ReverseCustomerAsync(entity, returnedValue, cancellationToken);
                break;
        }
    }

    private async Task ReverseCustomerAsync(ReturnRequest entity, decimal amount, CancellationToken cancellationToken)
    {
        var original = await _financialTitleRepository.FindActiveSourceTitleAsync(
            FinancialTitleType.Receivable,
            FinancialSourceType.SalesOrder,
            entity.OrderId!.Value,
            cancellationToken);

        var applied = ApplyCredit(original, entity, amount);
        var excess = Money.Round(amount - applied);

        FinancialTitle? refund = null;
        if (excess > 0)
        {
            var description = entity.FinancialTreatment == ReturnFinancialTreatment.CustomerCredit
                ? $"Customer credit for return {entity.ReturnNumber}"
                : $"Customer refund for return {entity.ReturnNumber}";
            refund = await CreateReturnTitleAsync(
                entity,
                FinancialTitleType.Payable,
                FinancialCounterpartyType.Customer,
                entity.CustomerId!.Value,
                entity.CustomerName!,
                description,
                excess,
                cancellationToken);
        }

        entity.MarkFinanciallyProcessed(applied > 0 ? original!.Id : null, applied, refund?.Id);
    }

    private async Task ReverseSupplierAsync(ReturnRequest entity, decimal amount, CancellationToken cancellationToken)
    {
        var original = await _financialTitleRepository.FindActiveSourceTitleAsync(
            FinancialTitleType.Payable,
            FinancialSourceType.PurchaseOrder,
            entity.PurchaseOrderId!.Value,
            cancellationToken);

        var applied = ApplyCredit(original, entity, amount);
        var excess = Money.Round(amount - applied);

        FinancialTitle? receivable = null;
        if (excess > 0)
        {
            receivable = await CreateReturnTitleAsync(
                entity,
                FinancialTitleType.Receivable,
                FinancialCounterpartyType.Supplier,
                entity.SupplierId!.Value,
                entity.SupplierName!,
                $"Supplier credit for return {entity.ReturnNumber}",
                excess,
                cancellationToken);
        }

        entity.MarkFinanciallyProcessed(applied > 0 ? original!.Id : null, applied, receivable?.Id);
    }

    private decimal ApplyCredit(FinancialTitle? original, ReturnRequest entity, decimal amount)
    {
        if (original is null || original.OutstandingBalance <= 0)
        {
            return 0m;
        }

        var result = original.ApplyReturnCredit(amount, entity.Id, entity.ReturnNumber, UtcNow());
        foreach (var settlement in result.Settlements)
        {
            _financialTitleRepository.StageSettlement(settlement);
        }

        return result.AppliedAmount;
    }

    private async Task<FinancialTitle> CreateReturnTitleAsync(
        ReturnRequest entity,
        FinancialTitleType type,
        FinancialCounterpartyType counterpartyType,
        Guid counterpartyId,
        string counterpartyName,
        string description,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (await _financialTitleRepository.ExistsForSourceAsync(FinancialSourceType.Return, entity.Id, cancellationToken))
        {
            throw new ReturnConflictException($"Return {entity.ReturnNumber} already generated a financial title.");
        }

        var now = UtcNow();
        var issueDate = DateOnly.FromDateTime(now);
        var dueDate = issueDate.AddDays(_financeOptions.DefaultReturnTitleDays);
        var title = FinancialTitle.Create(
            await FinancialTitleNumbers.NextAsync(_financialTitleRepository, now, cancellationToken),
            type,
            description,
            counterpartyType,
            counterpartyId,
            counterpartyName,
            FinancialSourceType.Return,
            entity.Id,
            entity.ReturnNumber,
            entity.ReturnNumber,
            issueDate,
            amount,
            notes: entity.FinancialNotes,
            createdByUserId: null,
            [(1, dueDate, amount)],
            now);

        if (await _financialTitleRepository.DocumentExistsAsync(title, cancellationToken))
        {
            throw new DuplicateFinancialDocumentException(entity.ReturnNumber);
        }

        await _financialTitleRepository.AddAsync(title, cancellationToken);
        return title;
    }

    private async Task<InventoryBalance> BalanceAsync(Guid productId, CancellationToken cancellationToken) =>
        await _inventoryRepository.GetTrackedByProductIdAsync(productId, cancellationToken)
            ?? throw new InventoryBalanceNotFoundException(productId);

    private static void EnsureAvailable(InventoryBalance balance, int quantity, string productName)
    {
        if (quantity > balance.AvailableQuantity)
        {
            throw new OrderInsufficientStockException(productName, balance.AvailableQuantity, quantity);
        }
    }

    private static ReturnItem ItemFor(ReturnRequest entity, ReturnCompletedItem item) =>
        entity.Items.SingleOrDefault(line => line.Id == item.ReturnItemId)
            ?? throw new ReturnConflictException("The completed item does not belong to this return.");

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
