using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface ISupplierRepository
{
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedTaxIdentifierAsync(
        string normalizedTaxIdentifier,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> GetAllAsync(
        bool? isActive = null,
        string? name = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
}
