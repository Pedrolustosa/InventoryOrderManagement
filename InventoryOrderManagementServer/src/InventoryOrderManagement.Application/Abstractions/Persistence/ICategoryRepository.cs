using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> HasActiveProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
