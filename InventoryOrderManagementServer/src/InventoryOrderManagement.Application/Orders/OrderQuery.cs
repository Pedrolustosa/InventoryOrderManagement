using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Orders;

public sealed class OrderQuery
{
    public Guid? CustomerId { get; init; }

    public OrderStatus? Status { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public string? OrderNumber { get; init; }
}
