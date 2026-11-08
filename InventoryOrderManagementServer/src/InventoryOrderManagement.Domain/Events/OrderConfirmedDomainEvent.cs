using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Domain.Events;

public sealed record OrderConfirmedItem(Guid ProductId, int Quantity);

public sealed record OrderConfirmedDomainEvent(
    Guid OrderId,
    IReadOnlyList<OrderConfirmedItem> Items,
    DateTime OccurredOnUtc) : IDomainEvent;
