using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Categories;

public sealed class DeactivateCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public DeactivateCategoryUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CategoryNotFoundException(id);

        if (await _categoryRepository.HasActiveProductsAsync(id, cancellationToken))
        {
            throw new CategoryHasActiveProductsException(id);
        }

        category.Deactivate();
        await _categoryRepository.UpdateAsync(category, cancellationToken);
    }
}
