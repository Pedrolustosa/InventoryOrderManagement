using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// One product line inside an order. Price, SKU and name are copies taken when the line is built.
/// </summary>
public class OrderItem
{
    private OrderItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    public static OrderItem Create(
        Guid productId,
        string productSku,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        var price = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductSku = NormalizeRequired(productSku, Product.SkuMaxLength, "Product SKU"),
            ProductName = NormalizeRequired(productName, Product.NameMaxLength, "Product name"),
            Quantity = quantity,
            UnitPrice = price,
            LineTotal = decimal.Round(quantity * price, 2, MidpointRounding.AwayFromZero)
        };
    }

    internal void AssignOrder(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order is required.");
        }

        OrderId = orderId;
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
