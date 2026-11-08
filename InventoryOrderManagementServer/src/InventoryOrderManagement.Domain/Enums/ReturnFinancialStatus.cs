namespace InventoryOrderManagement.Domain.Enums;

public enum ReturnFinancialStatus
{
    None = 0,
    Pending = 1,

    /// <summary>The financial reversal of the completed return was applied.</summary>
    Processed = 2
}
