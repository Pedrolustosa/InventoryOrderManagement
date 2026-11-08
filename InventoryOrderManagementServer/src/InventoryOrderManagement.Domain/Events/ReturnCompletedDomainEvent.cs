using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Events;

/// <summary>
/// A returned line as it was physically settled: accepted units for customer returns and exchanges
/// (split by disposition), dispatched units for supplier returns.
/// </summary>
public sealed record ReturnCompletedItem(
    Guid ReturnItemId,
    Guid ProductId,
    int Quantity,
    int RestockedQuantity,
    int QuarantinedQuantity,
    int DiscardedQuantity,
    ReturnItemCondition? Condition,
    decimal UnitValue);

/// <summary>A replacement line issued to the customer on an exchange.</summary>
public sealed record ReturnCompletedReplacement(
    Guid ReplacementItemId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Raised when a return is physically completed: a customer return or exchange whose inspection accepted
/// at least one unit, or a supplier return that was dispatched.
/// </summary>
public sealed record ReturnCompletedDomainEvent(
    Guid ReturnRequestId,
    ReturnType Type,
    Guid? OrderId,
    Guid? PurchaseOrderId,
    IReadOnlyList<ReturnCompletedItem> Items,
    IReadOnlyList<ReturnCompletedReplacement> Replacements,
    DateTime OccurredOnUtc) : IDomainEvent;
