using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class InvalidShipmentStatusException : Exception
{
    public InvalidShipmentStatusException(ShipmentStatus status)
        : base($"The shipment cannot change from status '{status}'.")
    {
        Status = status;
    }

    public ShipmentStatus Status { get; }
}
