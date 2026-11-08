namespace InventoryOrderManagement.Domain.Enums;

public enum StockMovementType
{
    Entry = 0,
    Exit = 1,
    AdjustmentIncrease = 2,
    AdjustmentDecrease = 3,
    OrderExit = 4,
    OrderCancellationReturn = 5,
    PurchaseEntry = 6,
    CustomerReturnEntry = 7,
    SupplierReturnExit = 8,
    OrderReservation = 9,
    OrderReservationRelease = 10,

    /// <summary>Customer-returned units held in quarantine. Record only: on-hand and available are unchanged.</summary>
    CustomerReturnQuarantine = 11,

    /// <summary>Customer-returned units discarded as loss/damage. Record only: on-hand and available are unchanged.</summary>
    CustomerReturnLoss = 12,

    /// <summary>Replacement units issued to the customer on an exchange.</summary>
    ExchangeReplacementExit = 13
}
