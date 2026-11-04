namespace InventoryOrderManagement.Application.Reports.Dtos;

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
