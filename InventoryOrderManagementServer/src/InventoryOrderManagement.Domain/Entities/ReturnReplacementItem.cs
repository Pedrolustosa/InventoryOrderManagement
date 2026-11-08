using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>A product sent to the customer in exchange for the returned items.</summary>
public class ReturnReplacementItem
{
    private ReturnReplacementItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReturnRequestId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPriceSnapshot { get; private set; }

    public decimal LineAmount { get; private set; }

    public static ReturnReplacementItem Create(
        Guid productId,
        string productSku,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Replacement product is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Replacement quantity must be a positive integer.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Replacement unit price cannot be negative.");
        }

        var price = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);

        return new ReturnReplacementItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductSku = NormalizeRequired(productSku, Product.SkuMaxLength, "Replacement product SKU"),
            ProductName = NormalizeRequired(productName, Product.NameMaxLength, "Replacement product name"),
            Quantity = quantity,
            UnitPriceSnapshot = price,
            LineAmount = decimal.Round(quantity * price, 2, MidpointRounding.AwayFromZero)
        };
    }

    internal void AssignReturnRequest(Guid returnRequestId)
    {
        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        ReturnRequestId = returnRequestId;
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
