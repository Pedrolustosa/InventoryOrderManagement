using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories.Dtos;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Categories;

public sealed class GetCategoryByIdUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CategoryNotFoundException(id);

        return CategoryResponse.FromEntity(category);
    }
}
