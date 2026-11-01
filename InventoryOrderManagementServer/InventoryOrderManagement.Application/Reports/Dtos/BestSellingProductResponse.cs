namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class BestSellingProductResponse
{
    public Guid ProductId { get; init; }

    public string ProductSku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public int TotalQuantitySold { get; init; }

    public decimal TotalConfirmedOrderValue { get; init; }

    public int TotalOrders { get; init; }
}
