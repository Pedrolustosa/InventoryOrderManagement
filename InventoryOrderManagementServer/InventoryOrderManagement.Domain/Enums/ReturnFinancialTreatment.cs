namespace InventoryOrderManagement.Domain.Enums;

/// <summary>
/// Intended financial handling. No title, settlement or cash movement is created automatically.
/// </summary>
public enum ReturnFinancialTreatment
{
    None = 0,
    CustomerRefund = 1,
    CustomerCredit = 2,
    SupplierCredit = 3,
    SupplierRefund = 4
}
