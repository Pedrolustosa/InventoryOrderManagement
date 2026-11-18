using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Events;

public sealed record OrderCancelledItem(Guid ProductId, int Quantity);

/// <summary>
/// Raised once when an order moves from Draft or Confirmed to Cancelled. <see cref="PreviousStatus"/> tells the
/// handler whether stock was committed (Confirmed) or not (Draft).
/// </summary>
public sealed record OrderCancelledDomainEvent(
    Guid OrderId,
    OrderStatus PreviousStatus,
    IReadOnlyList<OrderCancelledItem> Items,
    DateTime OccurredOnUtc) : IDomainEvent;
