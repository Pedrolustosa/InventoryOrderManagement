using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// One product line on a purchase order. SKU, name and unit cost are copies taken when the line is built.
/// </summary>
public class PurchaseOrderItem
{
    private PurchaseOrderItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid PurchaseOrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public int OrderedQuantity { get; private set; }

    public int ReceivedQuantity { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal LineTotal { get; private set; }

    public int PendingQuantity => OrderedQuantity - ReceivedQuantity;

    public static PurchaseOrderItem Create(
        Guid productId,
        string productSku,
        string productName,
        int orderedQuantity,
        decimal unitCost)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (orderedQuantity <= 0)
        {
            throw new DomainException("Ordered quantity must be a positive integer.");
        }

        if (unitCost < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }

        var cost = decimal.Round(unitCost, 2, MidpointRounding.AwayFromZero);

        return new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductSku = NormalizeRequired(productSku, Product.SkuMaxLength, "Product SKU"),
            ProductName = NormalizeRequired(productName, Product.NameMaxLength, "Product name"),
            OrderedQuantity = orderedQuantity,
            ReceivedQuantity = 0,
            UnitCost = cost,
            LineTotal = decimal.Round(orderedQuantity * cost, 2, MidpointRounding.AwayFromZero)
        };
    }

    internal void AssignPurchaseOrder(Guid purchaseOrderId)
    {
        if (purchaseOrderId == Guid.Empty)
        {
            throw new DomainException("Purchase order is required.");
        }

        PurchaseOrderId = purchaseOrderId;
    }

    internal void Receive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Received quantity must be a positive integer.");
        }

        if (quantity > PendingQuantity)
        {
            throw new PurchaseOrderConflictException("Received quantity cannot exceed the pending quantity.");
        }

        ReceivedQuantity += quantity;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
