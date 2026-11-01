using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Aggregate root representing a sellable product in the catalog.
/// Encapsulates creation invariants so invalid products cannot exist in memory.
/// </summary>
public class Product
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 500;

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public decimal CurrentPrice { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Product Create(
        string sku,
        string name,
        string? description,
        decimal currentPrice,
        Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        var normalizedSku = NormalizeSku(sku);
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);

        if (currentPrice < 0)
        {
            throw new DomainException("Current price cannot be negative.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Sku = normalizedSku,
            Name = normalizedName,
            Description = normalizedDescription,
            CurrentPrice = currentPrice,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        var normalized = sku.Trim().ToUpperInvariant();

        if (normalized.Length > SkuMaxLength)
        {
            throw new DomainException($"SKU cannot exceed {SkuMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > NameMaxLength)
        {
            throw new DomainException($"Name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();

        if (normalized.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Description cannot exceed {DescriptionMaxLength} characters.");
        }

        return normalized;
    }
}
