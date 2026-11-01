using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Products.Dtos;

public sealed class ProductResponse
{
    public required Guid Id { get; init; }

    public required string Sku { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required decimal CurrentPrice { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static ProductResponse FromEntity(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            CurrentPrice = product.CurrentPrice,
            IsActive = product.IsActive,
            CreatedAtUtc = product.CreatedAtUtc
        };
    }
}
