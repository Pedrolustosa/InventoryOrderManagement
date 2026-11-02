using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

/// <summary>
/// Raised when a purchase order operation does not match the current status.
/// Kept separate from <see cref="DomainException"/> so the API can map it to HTTP 409.
/// </summary>
public sealed class InvalidPurchaseOrderStatusException : Exception
{
    public InvalidPurchaseOrderStatusException(PurchaseOrderStatus status)
        : base($"The operation cannot be executed because the purchase order is {status}.")
    {
        Status = status;
    }

    public PurchaseOrderStatus Status { get; }
}
