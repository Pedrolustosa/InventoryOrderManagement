namespace InventoryOrderManagement.Application.Products.Dtos;

public sealed class CreateProductRequest
{
    public required string Sku { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public decimal CurrentPrice { get; init; }

    public required Guid CategoryId { get; init; }
}
