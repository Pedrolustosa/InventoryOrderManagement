using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a return operation does not match the current status.
/// Kept separate from <see cref="DomainException"/> so the API can map it to HTTP 409.
/// </summary>
public sealed class InvalidReturnStatusException : Exception
{
    public InvalidReturnStatusException(ReturnStatus status)
        : base($"The return cannot change from status '{status}'.")
    {
        Status = status;
    }

    public ReturnStatus Status { get; }
}
