using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Current on-hand quantity of a product in the single warehouse.
/// Quantity changes only through domain methods, which also advance <see cref="Version"/>
/// so concurrent writes can be detected.
/// </summary>
public class InventoryBalance
{
    private InventoryBalance()
    {
    }

    public Guid ProductId { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int MinimumStockLevel { get; private set; }

    public int Version { get; private set; }

    public bool IsLowStock => QuantityOnHand <= MinimumStockLevel;

    public static InventoryBalance Create(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        return new InventoryBalance
        {
            ProductId = productId,
            QuantityOnHand = 0,
            MinimumStockLevel = 0,
            Version = 0
        };
    }

    public void Receive(int quantity)
    {
        EnsurePositiveQuantity(quantity);
        EnsureNoOverflow(quantity);

        QuantityOnHand += quantity;
        IncrementVersion();
    }

    public void Issue(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > QuantityOnHand)
        {
            throw new InsufficientStockException(QuantityOnHand, quantity);
        }

        QuantityOnHand -= quantity;
        IncrementVersion();
    }

    /// <summary>
    /// Sets the on-hand quantity to the physical count.
    /// Returns null when the recorded quantity already matches, so no movement is required.
    /// </summary>
    public StockQuantityChange? AdjustTo(int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new DomainException("Physical quantity cannot be negative.");
        }

        if (newQuantity == QuantityOnHand)
        {
            return null;
        }

        var previousQuantity = QuantityOnHand;
        var difference = newQuantity - previousQuantity;
        QuantityOnHand = newQuantity;
        IncrementVersion();

        return new StockQuantityChange(
            difference > 0 ? StockMovementType.AdjustmentIncrease : StockMovementType.AdjustmentDecrease,
            Math.Abs(difference),
            previousQuantity,
            newQuantity);
    }

    public void SetMinimumStockLevel(int minimumStockLevel)
    {
        if (minimumStockLevel < 0)
        {
            throw new DomainException("Minimum stock level cannot be negative.");
        }

        if (minimumStockLevel == MinimumStockLevel)
        {
            return;
        }

        MinimumStockLevel = minimumStockLevel;
        IncrementVersion();
    }

    private void EnsureNoOverflow(int quantity)
    {
        if (QuantityOnHand > int.MaxValue - quantity)
        {
            throw new DomainException("Quantity exceeds the supported stock limit.");
        }
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be a positive integer.");
        }
    }

    private void IncrementVersion()
    {
        Version++;
    }
}
