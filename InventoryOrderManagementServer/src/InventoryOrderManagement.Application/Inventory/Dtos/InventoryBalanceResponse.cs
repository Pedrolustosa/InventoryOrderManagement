using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Domain.Entities;

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
        InventoryBalance balance)
    {
        return new InventoryBalanceResponse
        {
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            QuantityOnHand = balance.QuantityOnHand,
            ReservedQuantity = balance.ReservedQuantity,
            AvailableQuantity = balance.AvailableQuantity,
            MinimumStockLevel = balance.MinimumStockLevel,
            IsLowStock = balance.IsLowStock
        };
    }
}
