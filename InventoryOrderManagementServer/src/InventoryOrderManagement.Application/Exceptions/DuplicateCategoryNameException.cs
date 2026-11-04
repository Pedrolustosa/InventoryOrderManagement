namespace InventoryOrderManagement.Application.Exceptions;

public class DuplicateCategoryNameException : Exception
{
    public string Name { get; }

    public DuplicateCategoryNameException(string name)
        : base($"A category with name '{name}' already exists.")
    {
        Name = name;
    }
}
