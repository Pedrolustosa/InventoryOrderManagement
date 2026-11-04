namespace InventoryOrderManagement.Application.Exceptions;

public class InactiveCategoryException : Exception
{
    public Guid CategoryId { get; }

    public InactiveCategoryException(Guid categoryId)
        : base($"Category '{categoryId}' is inactive and cannot receive new products.")
    {
        CategoryId = categoryId;
    }
}
