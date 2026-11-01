namespace InventoryOrderManagement.Application.Reports.Dtos;

/// <summary>
/// CancellationRate uses the creation cohort: orders created in the window whose current status is Cancelled,
/// divided by every order created in the window. CancellationValue sums Subtotal of orders whose CancelledAtUtc
/// falls in the window. Those two dates answer different questions.
/// </summary>
public sealed class CancellationReportResponse
{
    public int OrdersCreated { get; init; }

    public int TotalCancelledOrders { get; init; }

    public decimal CancellationRate { get; init; }

    public int CancelledInPeriod { get; init; }

    public decimal CancellationValue { get; init; }

    public required IReadOnlyList<CancellationPeriodResponse> CancellationByPeriod { get; init; }
}

public sealed class CancellationPeriodResponse
{
    public string Period { get; init; } = null!;

    public int TotalCancelledOrders { get; init; }

    public decimal CancellationValue { get; init; }
}
