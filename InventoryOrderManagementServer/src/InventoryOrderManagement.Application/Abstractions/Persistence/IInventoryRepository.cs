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

    /// <summary>True when the shipment already issued stock (an OrderExit linked to it exists).</summary>
    Task<bool> ExistsShipmentExitAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the order has an OrderExit not linked to a shipment, i.e. stock issued at confirmation
    /// by the pre-reservation flow.
    /// </summary>
    Task<bool> ExistsLegacyOrderExitAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Net reserved quantity per product for the order (reservations minus releases).</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetNetReservedByProductAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDetails>> GetMovementsAsync(
        StockMovementQuery query,
        CancellationToken cancellationToken = default);
}
