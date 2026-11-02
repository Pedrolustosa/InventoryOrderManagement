using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a shipment operation does not match the current status.
/// Kept separate from <see cref="DomainException"/> so the API can map it to HTTP 409.
/// </summary>
public sealed class InvalidShipmentStatusException : Exception
{
    public InvalidShipmentStatusException(ShipmentStatus status)
        : base($"The shipment cannot change from status '{status}'.")
    {
        Status = status;
    }

    public ShipmentStatus Status { get; }
}
