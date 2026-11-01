using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for Product. Defined in Application so use cases depend on abstractions (DIP).
/// </summary>
public interface IProductRepository
{
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
}
