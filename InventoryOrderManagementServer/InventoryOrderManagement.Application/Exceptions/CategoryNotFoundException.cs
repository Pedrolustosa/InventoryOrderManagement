namespace InventoryOrderManagement.Application.Exceptions;

public class CategoryNotFoundException : Exception
{
    public Guid CategoryId { get; }

    public CategoryNotFoundException(Guid categoryId)
        : base($"Category '{categoryId}' was not found.")
    {
        CategoryId = categoryId;
    }
}
