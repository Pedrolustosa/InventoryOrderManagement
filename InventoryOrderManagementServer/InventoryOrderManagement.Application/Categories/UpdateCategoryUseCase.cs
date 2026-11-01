using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Categories;

public sealed class UpdateCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public UpdateCategoryUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task ExecuteAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CategoryNotFoundException(id);

        var normalizedName = Category.NormalizeNameForComparison(request.Name);

        if (await _categoryRepository.ExistsByNormalizedNameAsync(
                normalizedName,
                excludeId: id,
                cancellationToken))
        {
            throw new DuplicateCategoryNameException(request.Name.Trim());
        }

        category.Update(request.Name, request.Description);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
    }
}
