using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Shipment?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Shipment> Items, int TotalItems)> GetPageAsync(
        ShipmentQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class ShipmentQuery
{
    public ShipmentStatus? Status { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? CustomerId { get; init; }

    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
