namespace InventoryOrderManagement.Domain.Enums;

/// <summary>
/// Destination of inspected customer-return units. Only Restock increases available inventory.
/// Quarantine and Discard are historical classifications in this phase.
/// </summary>
public enum ReturnItemDisposition
{
    Restock = 0,
    Quarantine = 1,
    Discard = 2,
    Rejected = 3
}
