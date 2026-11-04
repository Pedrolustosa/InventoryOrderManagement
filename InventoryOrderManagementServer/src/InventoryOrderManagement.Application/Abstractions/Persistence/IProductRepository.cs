using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
}
