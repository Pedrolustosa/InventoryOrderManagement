using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Immutable record of a stock change. Normal application flows cannot edit or delete it.
/// </summary>
public class StockMovement
{
    public const int ReasonMaxLength = 300;

    private StockMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid? SupplierId { get; private set; }

    public Guid? OrderId { get; private set; }

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

        if (movementType is StockMovementType.OrderExit or StockMovementType.OrderCancellationReturn)
        {
            throw new DomainException("Order stock movements must include an order.");
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

        if (movementType is not (StockMovementType.OrderExit or StockMovementType.OrderCancellationReturn))
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

        if (movementType == StockMovementType.OrderExit)
        {
            EnsureResult(previousQuantity - quantity, newQuantity, "order exit");
        }
        else
        {
            EnsureResult(previousQuantity + quantity, newQuantity, "order cancellation return");
        }

        return Build(
            productId,
            supplierId: null,
            orderId,
            movementType,
            quantity,
            previousQuantity,
            newQuantity,
            NormalizeReason(reason, required: true));
    }

    private static StockMovement Build(
        Guid productId,
        Guid? supplierId,
        Guid? orderId,
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
