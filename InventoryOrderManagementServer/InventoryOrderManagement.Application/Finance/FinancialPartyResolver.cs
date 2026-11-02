using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Finance;

public sealed record ResolvedParty(
    FinancialCounterpartyType CounterpartyType,
    Guid? CounterpartyId,
    string CounterpartyName,
    FinancialSourceType SourceType,
    Guid? SourceId,
    string? SourceReference);

public sealed class FinancialPartyResolver
{
    private static readonly PurchaseOrderStatus[] PayableSources =
    [
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.Received
    ];

    private readonly ISupplierRepository _suppliers;
    private readonly ICustomerRepository _customers;
    private readonly IPurchaseOrderRepository _purchaseOrders;
    private readonly IOrderRepository _orders;

    public FinancialPartyResolver(
        ISupplierRepository suppliers,
        ICustomerRepository customers,
        IPurchaseOrderRepository purchaseOrders,
        IOrderRepository orders)
    {
        _suppliers = suppliers;
        _customers = customers;
        _purchaseOrders = purchaseOrders;
        _orders = orders;
    }

    public Task<ResolvedParty> ForPayableAsync(
        Guid? supplierId,
        string? counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        CancellationToken cancellationToken) =>
        ResolveAsync(
            FinancialTitleType.Payable,
            supplierId,
            counterpartyName,
            sourceType,
            sourceId,
            cancellationToken);

    public Task<ResolvedParty> ForReceivableAsync(
        Guid? customerId,
        string? counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        CancellationToken cancellationToken) =>
        ResolveAsync(
            FinancialTitleType.Receivable,
            customerId,
            counterpartyName,
            sourceType,
            sourceId,
            cancellationToken);

    private async Task<ResolvedParty> ResolveAsync(
        FinancialTitleType titleType,
        Guid? counterpartyId,
        string? counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        CancellationToken cancellationToken)
    {
        if (titleType == FinancialTitleType.Payable && sourceType == FinancialSourceType.SalesOrder)
        {
            throw new DomainException("A payable cannot originate from a sales order.");
        }

        if (titleType == FinancialTitleType.Receivable && sourceType == FinancialSourceType.PurchaseOrder)
        {
            throw new DomainException("A receivable cannot originate from a purchase order.");
        }

        string? sourceReference = null;
        if (sourceType == FinancialSourceType.PurchaseOrder)
        {
            if (sourceId is null)
            {
                throw new DomainException("Source document is required.");
            }

            var purchaseOrder = await _purchaseOrders.GetByIdAsync(sourceId.Value, cancellationToken)
                ?? throw new PurchaseOrderNotFoundException(sourceId.Value);
            if (!PayableSources.Contains(purchaseOrder.Status))
            {
                throw new InvalidPurchaseOrderStatusException(purchaseOrder.Status);
            }

            if (counterpartyId.HasValue && counterpartyId.Value != purchaseOrder.SupplierId)
            {
                throw new DomainException("The payable supplier must match the purchase order.");
            }

            counterpartyId = purchaseOrder.SupplierId;
            sourceReference = purchaseOrder.PurchaseOrderNumber;
        }
        else if (sourceType == FinancialSourceType.SalesOrder)
        {
            if (sourceId is null)
            {
                throw new DomainException("Source document is required.");
            }

            var order = await _orders.GetByIdAsync(sourceId.Value, cancellationToken)
                ?? throw new OrderNotFoundException(sourceId.Value);
            if (order.Status != OrderStatus.Delivered)
            {
                throw new InvalidOrderStatusException(order.Status);
            }

            if (counterpartyId.HasValue && counterpartyId.Value != order.CustomerId)
            {
                throw new DomainException("The receivable customer must match the sales order.");
            }

            counterpartyId = order.CustomerId;
            sourceReference = order.OrderNumber;
        }

        if (counterpartyId.HasValue)
        {
            if (titleType == FinancialTitleType.Payable)
            {
                var supplier = await _suppliers.GetByIdAsync(counterpartyId.Value, cancellationToken)
                    ?? throw new SupplierNotFoundException(counterpartyId.Value);
                if (!supplier.IsActive)
                {
                    throw new InactiveSupplierFinanceException(supplier.Id);
                }

                return new ResolvedParty(
                    FinancialCounterpartyType.Supplier,
                    supplier.Id,
                    supplier.Name,
                    sourceType,
                    sourceType == FinancialSourceType.Manual ? null : sourceId,
                    sourceReference);
            }

            var customer = await _customers.GetByIdAsync(counterpartyId.Value, cancellationToken)
                ?? throw new CustomerNotFoundException(counterpartyId.Value);
            if (!customer.IsActive)
            {
                throw new InactiveCustomerFinanceException(customer.Id);
            }

            return new ResolvedParty(
                FinancialCounterpartyType.Customer,
                customer.Id,
                customer.Name,
                sourceType,
                sourceType == FinancialSourceType.Manual ? null : sourceId,
                sourceReference);
        }

        if (sourceType != FinancialSourceType.Manual)
        {
            throw new DomainException("A linked title requires a supplier or customer.");
        }

        return new ResolvedParty(
            FinancialCounterpartyType.Other,
            null,
            counterpartyName ?? string.Empty,
            FinancialSourceType.Manual,
            null,
            null);
    }
}
