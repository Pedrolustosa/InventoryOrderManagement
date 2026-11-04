using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Purchases;

internal sealed class PurchaseDraftBuilder
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;

    public PurchaseDraftBuilder(ISupplierRepository supplierRepository, IProductRepository productRepository)
    {
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
    }

    public async Task<PreparedPurchaseDraft> PrepareAsync(
        Guid supplierId,
        IReadOnlyList<PurchaseOrderItemRequest>? items,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new SupplierNotFoundException(supplierId);

        if (!supplier.IsActive)
        {
            throw new InactiveSupplierPurchaseException(supplier.Id);
        }

        return new PreparedPurchaseDraft(supplier, await BuildItemsAsync(items, cancellationToken));
    }

    public Task<IReadOnlyList<PurchaseOrderItem>> BuildItemsAsync(
        IReadOnlyList<PurchaseOrderItemRequest>? items,
        CancellationToken cancellationToken)
    {
        return BuildItemsCoreAsync(items, cancellationToken);
    }

    private async Task<IReadOnlyList<PurchaseOrderItem>> BuildItemsCoreAsync(
        IReadOnlyList<PurchaseOrderItemRequest>? items,
        CancellationToken cancellationToken)
    {
        if (items is null || items.Count == 0)
        {
            throw new DomainException("A purchase order must contain at least one item.");
        }

        var lines = new List<PurchaseOrderItem>();
        foreach (var group in items.GroupBy(item => item.ProductId))
        {
            var costs = group
                .Select(item => decimal.Round(item.UnitCost, 2, MidpointRounding.AwayFromZero))
                .Distinct()
                .ToList();

            if (costs.Count > 1)
            {
                throw new PurchaseOrderConflictException("The same product cannot have different unit costs on a purchase order.");
            }

            var quantity = 0;
            foreach (var item in group)
            {
                if (item.OrderedQuantity <= 0 || quantity > int.MaxValue - item.OrderedQuantity)
                {
                    throw new DomainException("Ordered quantity must be a positive integer.");
                }

                quantity += item.OrderedQuantity;
            }

            var product = await _productRepository.GetByIdAsync(group.Key, cancellationToken)
                ?? throw new ProductNotFoundException(group.Key);

            if (!product.IsActive)
            {
                throw new InactiveProductPurchaseException(product.Name);
            }

            lines.Add(PurchaseOrderItem.Create(product.Id, product.Sku, product.Name, quantity, costs[0]));
        }

        return lines;
    }
}

internal sealed record PreparedPurchaseDraft(Supplier Supplier, IReadOnlyList<PurchaseOrderItem> Items);
