using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Aggregate root representing a product category in the catalog.
/// </summary>
public class Category
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;

    private Category()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Category Create(string name, string? description)
    {
        var (displayName, normalizedName) = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            NormalizedName = normalizedName,
            Description = normalizedDescription,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    public void Update(string name, string? description)
    {
        var (displayName, normalizedName) = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);

        Name = displayName;
        NormalizedName = normalizedName;
        Description = normalizedDescription;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Category is already inactive.");
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static string NormalizeNameForComparison(string name)
    {
        return NormalizeName(name).NormalizedName;
    }

    private static (string DisplayName, string NormalizedName) NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var displayName = name.Trim();

        if (displayName.Length > NameMaxLength)
        {
            throw new DomainException($"Name cannot exceed {NameMaxLength} characters.");
        }

        return (displayName, displayName.ToUpperInvariant());
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
