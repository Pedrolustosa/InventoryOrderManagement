namespace InventoryOrderManagement.Application.Abstractions.Reports;

public interface ISalesReportsQuery
{
    Task<IReadOnlyList<SoldItemRow>> GetSoldItemsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSaleRow>> GetCustomerSalesAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default);
}

public sealed class SoldItemRow
{
    public Guid ProductId { get; init; }

    public string ProductSku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public int Quantity { get; init; }

    public decimal LineTotal { get; init; }

    public Guid OrderId { get; init; }

    public DateTime ConfirmedAtUtc { get; init; }
}

public sealed class CustomerSaleRow
{
    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = null!;

    public decimal Subtotal { get; init; }

    public DateTime ConfirmedAtUtc { get; init; }

    public int ItemQuantity { get; init; }
}
