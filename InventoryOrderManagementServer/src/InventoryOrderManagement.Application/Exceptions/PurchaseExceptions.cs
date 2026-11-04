namespace InventoryOrderManagement.Application.Exceptions;

public sealed class PurchaseOrderNotFoundException : Exception
{
    public PurchaseOrderNotFoundException(Guid purchaseOrderId)
        : base($"Purchase order '{purchaseOrderId}' was not found.")
    {
        PurchaseOrderId = purchaseOrderId;
    }

    public Guid PurchaseOrderId { get; }
}

public sealed class PurchaseReceiptNotFoundException : Exception
{
    public PurchaseReceiptNotFoundException(Guid receiptId)
        : base($"Purchase receipt '{receiptId}' was not found.")
    {
        ReceiptId = receiptId;
    }

    public Guid ReceiptId { get; }
}

public sealed class InactiveSupplierPurchaseException : Exception
{
    public InactiveSupplierPurchaseException(Guid supplierId)
        : base($"Supplier '{supplierId}' is inactive and cannot be used on a purchase order.")
    {
        SupplierId = supplierId;
    }

    public Guid SupplierId { get; }
}

public sealed class InactiveProductPurchaseException : Exception
{
    public InactiveProductPurchaseException(string productName)
        : base($"Product '{productName}' is inactive and cannot be purchased.")
    {
        ProductName = productName;
    }

    public string ProductName { get; }
}

public sealed class InvalidPurchasePeriodException : Exception
{
    public InvalidPurchasePeriodException()
        : base("The start date must be earlier than or equal to the end date.")
    {
    }
}
