using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Categories;

public sealed class CreateCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> ExecuteAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = Category.Create(request.Name, request.Description);

        if (await _categoryRepository.ExistsByNormalizedNameAsync(
                category.NormalizedName,
                excludeId: null,
                cancellationToken))
        {
            throw new DuplicateCategoryNameException(category.Name);
        }

        await _categoryRepository.AddAsync(category, cancellationToken);

        return CategoryResponse.FromEntity(category);
    }
}
