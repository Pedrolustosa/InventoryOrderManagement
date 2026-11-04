using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class InvalidOrderStatusException : Exception
{
    public InvalidOrderStatusException(OrderStatus status)
        : base($"The operation cannot be executed because the order is {status}.")
    {
        Status = status;
    }

    public OrderStatus Status { get; }
}
