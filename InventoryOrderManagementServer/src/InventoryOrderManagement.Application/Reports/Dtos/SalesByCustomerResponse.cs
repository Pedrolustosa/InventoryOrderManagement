namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class SalesByCustomerResponse
{
    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = null!;

    public int TotalOrders { get; init; }

    public int TotalItemsSold { get; init; }

    public decimal TotalConfirmedOrderValue { get; init; }

    public decimal AverageOrderValue { get; init; }
}
