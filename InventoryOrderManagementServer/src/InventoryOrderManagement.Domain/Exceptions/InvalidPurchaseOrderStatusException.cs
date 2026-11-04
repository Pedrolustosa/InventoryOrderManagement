using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Exceptions;

public sealed class InvalidPurchaseOrderStatusException : Exception
{
    public InvalidPurchaseOrderStatusException(PurchaseOrderStatus status)
        : base($"The operation cannot be executed because the purchase order is {status}.")
    {
        Status = status;
    }

    public PurchaseOrderStatus Status { get; }
}
