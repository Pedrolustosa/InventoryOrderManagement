namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a shipment operation conflicts with current state or order linkage.
/// The API maps it to HTTP 409.
/// </summary>
public sealed class ShipmentConflictException : Exception
{
    public ShipmentConflictException(string message)
        : base(message)
    {
    }
}
