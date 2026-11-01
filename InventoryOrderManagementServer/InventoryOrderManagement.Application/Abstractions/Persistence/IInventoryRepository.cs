using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<InventoryBalance?> GetTrackedByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<InventoryBalanceDetails?> GetDetailsByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBalanceDetails>> GetBalancesAsync(
        Guid? productId,
        bool? isLowStock,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryBalance balance, CancellationToken cancellationToken = default);

    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDetails>> GetMovementsAsync(
        StockMovementQuery query,
        CancellationToken cancellationToken = default);
}
