namespace InventoryOrderManagement.Domain.Enums;

public enum ReturnType
{
    CustomerReturn = 0,
    SupplierReturn = 1,

    /// <summary>Customer return in which the customer receives replacement products.</summary>
    Exchange = 2
}
