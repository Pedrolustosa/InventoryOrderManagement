using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class StockMovementResponse
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public Guid? OrderId { get; init; }

    public required StockMovementType MovementType { get; init; }

    public required int Quantity { get; init; }

    public required int PreviousQuantity { get; init; }

    public required int NewQuantity { get; init; }

    public string? Reason { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static StockMovementResponse FromDetails(StockMovementDetails details)
    {
        return new StockMovementResponse
        {
            Id = details.Id,
            ProductId = details.ProductId,
            Sku = details.Sku,
            ProductName = details.ProductName,
            SupplierId = details.SupplierId,
            SupplierName = details.SupplierName,
            OrderId = details.OrderId,
            MovementType = details.MovementType,
            Quantity = details.Quantity,
            PreviousQuantity = details.PreviousQuantity,
            NewQuantity = details.NewQuantity,
            Reason = details.Reason,
            CreatedAtUtc = details.CreatedAtUtc
        };
    }
}
