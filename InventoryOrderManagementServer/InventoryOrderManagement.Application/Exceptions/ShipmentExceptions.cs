namespace InventoryOrderManagement.Application.Exceptions;

public sealed class ShipmentNotFoundException : Exception
{
    public ShipmentNotFoundException(Guid shipmentId)
        : base($"Shipment '{shipmentId}' was not found.")
    {
        ShipmentId = shipmentId;
    }

    public Guid ShipmentId { get; }
}

public sealed class InvalidShipmentPeriodException : Exception
{
    public InvalidShipmentPeriodException()
        : base("The start date must be earlier than or equal to the end date.")
    {
    }
}
