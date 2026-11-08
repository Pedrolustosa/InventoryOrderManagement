using InventoryOrderManagement.Domain.Abstractions;

namespace InventoryOrderManagement.Domain.Events;

/// <summary>
/// Raised when a product's available quantity crosses below its minimum stock level (it was at or above the
/// minimum before the change and is below it after). Staying below does not raise it again.
/// </summary>
public sealed record StockBelowMinimumDomainEvent(
    Guid ProductId,
    int Available,
    int MinimumStock,
    DateTime OccurredOnUtc) : IDomainEvent;
