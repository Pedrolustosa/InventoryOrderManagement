using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Append-only status change record for a shipment. Created only through <see cref="Shipment"/> transitions.
/// </summary>
public class ShipmentStatusHistory
{
    public const int NotesMaxLength = 500;

    private ShipmentStatusHistory()
    {
    }

    public Guid Id { get; private set; }

    public Guid ShipmentId { get; private set; }

    public ShipmentStatus? PreviousStatus { get; private set; }

    public ShipmentStatus NewStatus { get; private set; }

    public string? Notes { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    internal static ShipmentStatusHistory Create(
        Guid shipmentId,
        ShipmentStatus? previousStatus,
        ShipmentStatus newStatus,
        string? notes,
        Guid? changedByUserId)
    {
        return new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Notes = notes,
            OccurredAtUtc = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
    }
}
