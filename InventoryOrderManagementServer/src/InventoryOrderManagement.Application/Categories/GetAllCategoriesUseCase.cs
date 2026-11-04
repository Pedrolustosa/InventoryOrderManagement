using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories.Dtos;

namespace InventoryOrderManagement.Application.Categories;

public sealed class GetAllCategoriesUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public GetAllCategoriesUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ExecuteAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(isActive, cancellationToken);

        return categories
            .Select(CategoryResponse.FromEntity)
            .ToList();
    }
}
