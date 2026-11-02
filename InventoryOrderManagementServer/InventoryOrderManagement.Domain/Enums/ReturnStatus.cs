namespace InventoryOrderManagement.Domain.Enums;

public enum ReturnStatus
{
    Draft = 0,
    Authorized = 1,
    Received = 2,
    UnderInspection = 3,
    Completed = 4,
    Dispatched = 5,
    Rejected = 6,
    Cancelled = 7
}
