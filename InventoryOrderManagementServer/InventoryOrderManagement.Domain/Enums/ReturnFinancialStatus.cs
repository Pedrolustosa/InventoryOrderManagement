namespace InventoryOrderManagement.Domain.Enums;

/// <summary>
/// Financial follow-up status on a return. Pending means a future Finance operation is expected.
/// </summary>
public enum ReturnFinancialStatus
{
    None = 0,
    Pending = 1
}
