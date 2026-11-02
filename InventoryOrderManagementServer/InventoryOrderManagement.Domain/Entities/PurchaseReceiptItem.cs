using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// One product line inside a purchase receipt. The unit cost is copied from the purchase order item.
/// </summary>
public class PurchaseReceiptItem
{
    private PurchaseReceiptItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid PurchaseReceiptId { get; private set; }

    public Guid PurchaseOrderItemId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public int QuantityReceived { get; private set; }

    public decimal UnitCostSnapshot { get; private set; }

    public static PurchaseReceiptItem Create(
        Guid purchaseOrderItemId,
        Guid productId,
        string productSku,
        string productName,
        int quantityReceived,
        decimal unitCostSnapshot)
    {
        if (purchaseOrderItemId == Guid.Empty)
        {
            throw new DomainException("Purchase order item is required.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (quantityReceived <= 0)
        {
            throw new DomainException("Received quantity must be a positive integer.");
        }

        if (unitCostSnapshot < 0)
        {
            throw new DomainException("Unit cost cannot be negative.");
        }

        return new PurchaseReceiptItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderItemId = purchaseOrderItemId,
            ProductId = productId,
            ProductSku = NormalizeRequired(productSku, Product.SkuMaxLength, "Product SKU"),
            ProductName = NormalizeRequired(productName, Product.NameMaxLength, "Product name"),
            QuantityReceived = quantityReceived,
            UnitCostSnapshot = decimal.Round(unitCostSnapshot, 2, MidpointRounding.AwayFromZero)
        };
    }

    internal void AssignReceipt(Guid purchaseReceiptId)
    {
        if (purchaseReceiptId == Guid.Empty)
        {
            throw new DomainException("Purchase receipt is required.");
        }

        PurchaseReceiptId = purchaseReceiptId;
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
