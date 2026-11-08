using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Domain.Events;

public sealed record ShipmentDispatchedItem(Guid ProductId, int Quantity);

public sealed record ShipmentDispatchedDomainEvent(
    Guid ShipmentId,
    Guid OrderId,
    IReadOnlyList<ShipmentDispatchedItem> Items,
    DateTime OccurredOnUtc) : IDomainEvent;
