namespace InventoryOrderManagement.Domain.Enums;

public enum FinancialSourceType
{
    Manual = 0,
    PurchaseOrder = 1,
    SalesOrder = 2,

    /// <summary>System-generated title for a completed return (refund, supplier credit or exchange difference).</summary>
    Return = 3
}
