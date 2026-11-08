using InventoryOrderManagement.Application.Inventory;

namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class InventoryBalanceResponse
{
    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public required int QuantityOnHand { get; init; }

    public required int ReservedQuantity { get; init; }

    public required int AvailableQuantity { get; init; }

    public required int MinimumStockLevel { get; init; }

    public required bool IsLowStock { get; init; }

    public static InventoryBalanceResponse FromDetails(InventoryBalanceDetails details)
    {
        return new InventoryBalanceResponse
        {
            ProductId = details.ProductId,
            Sku = details.Sku,
            ProductName = details.ProductName,
            QuantityOnHand = details.QuantityOnHand,
            ReservedQuantity = details.ReservedQuantity,
            AvailableQuantity = details.AvailableQuantity,
            MinimumStockLevel = details.MinimumStockLevel,
            IsLowStock = details.IsLowStock
        };
    }

    public static InventoryBalanceResponse From(
        Guid productId,
        string sku,
        string productName,
        int quantityOnHand,
        int minimumStockLevel,
        int reservedQuantity = 0)
    {
        return new InventoryBalanceResponse
        {
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            QuantityOnHand = quantityOnHand,
            ReservedQuantity = reservedQuantity,
            AvailableQuantity = quantityOnHand - reservedQuantity,
            MinimumStockLevel = minimumStockLevel,
            IsLowStock = quantityOnHand <= minimumStockLevel
        };
    }
}
