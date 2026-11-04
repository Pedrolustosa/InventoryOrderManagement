using InventoryOrderManagement.Application.Inventory;

namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class LowStockProductResponse
{
    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public required int QuantityOnHand { get; init; }

    public required int MinimumStockLevel { get; init; }

    public required bool IsLowStock { get; init; }

    public static LowStockProductResponse FromDetails(InventoryBalanceDetails details)
    {
        return new LowStockProductResponse
        {
            ProductId = details.ProductId,
            Sku = details.Sku,
            ProductName = details.ProductName,
            QuantityOnHand = details.QuantityOnHand,
            MinimumStockLevel = details.MinimumStockLevel,
            IsLowStock = details.IsLowStock
        };
    }
}
