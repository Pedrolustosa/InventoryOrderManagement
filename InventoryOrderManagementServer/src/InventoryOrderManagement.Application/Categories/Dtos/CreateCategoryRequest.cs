namespace InventoryOrderManagement.Application.Categories.Dtos;

public sealed class CreateCategoryRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
