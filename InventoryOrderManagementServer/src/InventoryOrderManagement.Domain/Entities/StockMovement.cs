using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

public class StockMovement : AggregateRoot
{
    public const int ReasonMaxLength = 300;

    private StockMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid? SupplierId { get; private set; }

    public Guid? OrderId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }

    public Guid? PurchaseReceiptId { get; private set; }

    public Guid? ReturnRequestId { get; private set; }

    public Guid? ReturnItemId { get; private set; }

    public StockMovementType MovementType { get; private set; }

    public int Quantity { get; private set; }

    public int PreviousQuantity { get; private set; }

    public int NewQuantity { get; private set; }

    public string? Reason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static StockMovement Create(
        Guid productId,
        Guid? supplierId,
        StockMovementType movementType,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (!Enum.IsDefined(movementType))
        {
            throw new DomainException("Movement type is invalid.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        if (movementType is StockMovementType.OrderExit
            or StockMovementType.OrderCancellationReturn
            or StockMovementType.OrderReservation
            or StockMovementType.OrderReservationRelease
            or StockMovementType.PurchaseEntry
            or StockMovementType.CustomerReturnEntry
            or StockMovementType.CustomerReturnQuarantine
            or StockMovementType.CustomerReturnLoss
            or StockMovementType.ExchangeReplacementExit
            or StockMovementType.SupplierReturnExit)
        {
            throw new DomainException("This movement type must be created with its own reference.");
        }

        var normalizedSupplierId = NormalizeSupplierId(supplierId);
        var reasonIsRequired = movementType is not StockMovementType.Entry;
        var normalizedReason = NormalizeReason(reason, reasonIsRequired);

        switch (movementType)
        {
            case StockMovementType.Entry:
                if (normalizedSupplierId is null)
                {
                    throw new DomainException("A stock entry requires a supplier.");
                }

                EnsureResult(previousQuantity + quantity, newQuantity, "entry");
                break;
            case StockMovementType.Exit:
                EnsureResult(previousQuantity - quantity, newQuantity, "exit");
                break;
            case StockMovementType.AdjustmentIncrease:
                EnsureResult(previousQuantity + quantity, newQuantity, "adjustment");
                break;
            case StockMovementType.AdjustmentDecrease:
                EnsureResult(previousQuantity - quantity, newQuantity, "adjustment");
                break;
        }

        return Build(
            productId,
            normalizedSupplierId,
            orderId: null,
            purchaseOrderId: null,
            purchaseReceiptId: null,
            returnRequestId: null,
            returnItemId: null,
            movementType,
            quantity,
            previousQuantity,
            newQuantity,
            normalizedReason);
    }

    public static StockMovement CreateForOrder(
        Guid productId,
        Guid orderId,
        StockMovementType movementType,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string reason)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order is required.");
        }

        if (movementType is not (StockMovementType.OrderExit
            or StockMovementType.OrderCancellationReturn
            or StockMovementType.OrderReservation
            or StockMovementType.OrderReservationRelease))
        {
            throw new DomainException("This movement type is not an order movement.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        switch (movementType)
        {
            case StockMovementType.OrderExit:
                EnsureResult(previousQuantity - quantity, newQuantity, "order exit");
                break;
            case StockMovementType.OrderCancellationReturn:
                EnsureResult(previousQuantity + quantity, newQuantity, "order cancellation return");
                break;
            case StockMovementType.OrderReservation:
                EnsureResult(previousQuantity + quantity, newQuantity, "order reservation");
                break;
            case StockMovementType.OrderReservationRelease:
                EnsureResult(previousQuantity - quantity, newQuantity, "order reservation release");
                break;
        }

        return Build(
            productId,
            supplierId: null,
            orderId,
            purchaseOrderId: null,
            purchaseReceiptId: null,
            returnRequestId: null,
            returnItemId: null,
            movementType,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: true));
    }

    /// <summary>
    /// Creates the stock exit recorded when a shipment is dispatched. The movement is linked to both the
    /// sales order and the shipment, so a shipment can never issue stock twice for the same product.
    /// </summary>
    public static StockMovement CreateForShipment(
        Guid productId,
        Guid orderId,
        Guid shipmentId,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string reason)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new DomainException("Shipment is required.");
        }

        var movement = CreateForOrder(
            productId,
            orderId,
            StockMovementType.OrderExit,
            quantity,
            previousQuantity,
            newQuantity,
            reason);
        movement.ShipmentId = shipmentId;
        return movement;
    }

    public static StockMovement CreateForPurchase(
        Guid productId,
        Guid supplierId,
        Guid purchaseOrderId,
        Guid purchaseReceiptId,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (supplierId == Guid.Empty)
        {
            throw new DomainException("A stock entry requires a supplier.");
        }

        if (purchaseOrderId == Guid.Empty)
        {
            throw new DomainException("Purchase order is required.");
        }

        if (purchaseReceiptId == Guid.Empty)
        {
            throw new DomainException("Purchase receipt is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        EnsureResult(previousQuantity + quantity, newQuantity, "purchase entry");

        return Build(
            productId,
            supplierId,
            orderId: null,
            purchaseOrderId,
            purchaseReceiptId,
            returnRequestId: null,
            returnItemId: null,
            StockMovementType.PurchaseEntry,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: false));
    }

    public static StockMovement CreateForCustomerReturn(
        Guid productId,
        Guid orderId,
        Guid returnRequestId,
        Guid returnItemId,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason,
        StockMovementType movementType = StockMovementType.CustomerReturnEntry)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order is required.");
        }

        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        if (returnItemId == Guid.Empty)
        {
            throw new DomainException("Return item is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        switch (movementType)
        {
            case StockMovementType.CustomerReturnEntry:
                EnsureResult(previousQuantity + quantity, newQuantity, "customer return entry");
                break;
            case StockMovementType.CustomerReturnQuarantine:
            case StockMovementType.CustomerReturnLoss:
                // Record only: quarantined or discarded units never reach the sellable balance.
                EnsureResult(previousQuantity, newQuantity, "customer return quarantine/loss record");
                break;
            default:
                throw new DomainException("This movement type is not a customer return movement.");
        }

        return Build(
            productId,
            supplierId: null,
            orderId,
            purchaseOrderId: null,
            purchaseReceiptId: null,
            returnRequestId,
            returnItemId,
            movementType,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: false));
    }

    public static StockMovement CreateForExchangeReplacement(
        Guid productId,
        Guid orderId,
        Guid returnRequestId,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order is required.");
        }

        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        EnsureResult(previousQuantity - quantity, newQuantity, "exchange replacement exit");

        return Build(
            productId,
            supplierId: null,
            orderId,
            purchaseOrderId: null,
            purchaseReceiptId: null,
            returnRequestId,
            returnItemId: null,
            StockMovementType.ExchangeReplacementExit,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: false));
    }

    public static StockMovement CreateForSupplierReturn(
        Guid productId,
        Guid supplierId,
        Guid purchaseOrderId,
        Guid returnRequestId,
        Guid returnItemId,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (supplierId == Guid.Empty)
        {
            throw new DomainException("Supplier is required.");
        }

        if (purchaseOrderId == Guid.Empty)
        {
            throw new DomainException("Purchase order is required.");
        }

        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        if (returnItemId == Guid.Empty)
        {
            throw new DomainException("Return item is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }

        if (previousQuantity < 0 || newQuantity < 0)
        {
            throw new DomainException("Stock quantities cannot be negative.");
        }

        EnsureResult(previousQuantity - quantity, newQuantity, "supplier return exit");

        return Build(
            productId,
            supplierId,
            orderId: null,
            purchaseOrderId,
            purchaseReceiptId: null,
            returnRequestId,
            returnItemId,
            StockMovementType.SupplierReturnExit,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: false));
    }

    private static StockMovement Build(
        Guid productId,
        Guid? supplierId,
        Guid? orderId,
        Guid? purchaseOrderId,
        Guid? purchaseReceiptId,
        Guid? returnRequestId,
        Guid? returnItemId,
        StockMovementType movementType,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? reason)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SupplierId = supplierId,
            OrderId = orderId,
            PurchaseOrderId = purchaseOrderId,
            PurchaseReceiptId = purchaseReceiptId,
            ReturnRequestId = returnRequestId,
            ReturnItemId = returnItemId,
            MovementType = movementType,
            Quantity = quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            Reason = reason,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Guid? NormalizeSupplierId(Guid? supplierId)
    {
        if (supplierId is null || supplierId == Guid.Empty)
        {
            return null;
        }

        return supplierId;
    }

    private static string? NormalizeReason(string? reason, bool required)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            if (required)
            {
                throw new DomainException("Reason is required.");
            }

            return null;
        }

        var normalized = reason.Trim();

        if (normalized.Length > ReasonMaxLength)
        {
            throw new DomainException($"Reason cannot exceed {ReasonMaxLength} characters.");
        }

        return normalized;
    }

    private static void EnsureResult(int expectedNewQuantity, int newQuantity, string operation)
    {
        if (expectedNewQuantity != newQuantity)
        {
            throw new DomainException($"The resulting quantity does not match the {operation}.");
        }
    }
}
