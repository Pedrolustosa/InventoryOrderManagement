using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class InvalidReturnStatusException : Exception
{
    public InvalidReturnStatusException(ReturnStatus status)
        : base($"The return cannot change from status '{status}'.")
    {
        Status = status;
    }

    public ReturnStatus Status { get; }
}
