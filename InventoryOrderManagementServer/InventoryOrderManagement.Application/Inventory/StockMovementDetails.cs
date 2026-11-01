using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Inventory;

/// <summary>
/// Read model for a stock movement joined with product and supplier names.
/// </summary>
public sealed class StockMovementDetails
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public required StockMovementType MovementType { get; init; }

    public required int Quantity { get; init; }

    public required int PreviousQuantity { get; init; }

    public required int NewQuantity { get; init; }

    public string? Reason { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
