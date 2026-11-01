using InventoryOrderManagement.Application.Inventory;

namespace InventoryOrderManagement.Application.Inventory.Dtos;

public sealed class InventoryBalanceResponse
{
    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public required int QuantityOnHand { get; init; }

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
            MinimumStockLevel = details.MinimumStockLevel,
            IsLowStock = details.IsLowStock
        };
    }

    public static InventoryBalanceResponse From(
        Guid productId,
        string sku,
        string productName,
        int quantityOnHand,
        int minimumStockLevel)
    {
        return new InventoryBalanceResponse
        {
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            QuantityOnHand = quantityOnHand,
            MinimumStockLevel = minimumStockLevel,
            IsLowStock = quantityOnHand <= minimumStockLevel
        };
    }
}
