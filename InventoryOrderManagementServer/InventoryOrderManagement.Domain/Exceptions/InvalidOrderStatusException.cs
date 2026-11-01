using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when an order operation does not match the current status.
/// Kept separate from <see cref="DomainException"/> so the API can map it to HTTP 409.
/// </summary>
public sealed class InvalidOrderStatusException : Exception
{
    public InvalidOrderStatusException(OrderStatus status)
        : base($"The operation cannot be executed because the order is {status}.")
    {
        Status = status;
    }

    public OrderStatus Status { get; }
}
