namespace InventoryOrderManagement.Application.Categories.Dtos;

public sealed class UpdateCategoryRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
