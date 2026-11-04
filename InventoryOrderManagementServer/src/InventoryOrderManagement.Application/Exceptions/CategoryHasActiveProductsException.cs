namespace InventoryOrderManagement.Application.Exceptions;

public class CategoryHasActiveProductsException : Exception
{
    public Guid CategoryId { get; }

    public CategoryHasActiveProductsException(Guid categoryId)
        : base($"Category '{categoryId}' cannot be deactivated while it has active products.")
    {
        CategoryId = categoryId;
    }
}
