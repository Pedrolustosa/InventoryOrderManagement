namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class ShipmentConflictException : Exception
{
    public ShipmentConflictException(string message)
        : base(message)
    {
    }
}
