using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedTaxIdentifierAsync(
        string normalizedTaxIdentifier,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllAsync(
        bool? isActive = null,
        string? name = null,
        CustomerType? customerType = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}
