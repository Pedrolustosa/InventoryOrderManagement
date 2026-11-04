using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class StockMovementReportItemResponse
{
    public Guid MovementId { get; init; }

    public Guid ProductId { get; init; }

    public string ProductSku { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public Guid? OrderId { get; init; }

    public StockMovementType MovementType { get; init; }

    public int Quantity { get; init; }

    public int PreviousQuantity { get; init; }

    public int NewQuantity { get; init; }

    public string? Reason { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class StockMovementTotalsResponse
{
    public int TotalEntries { get; init; }

    public int TotalExit { get; init; }

    public int TotalAdjustmentIncrease { get; init; }

    public int TotalAdjustmentDecrease { get; init; }

    public int TotalOrderExit { get; init; }

    public int TotalOrderCancellationReturn { get; init; }
}

public sealed class StockMovementPeriodResponse
{
    public string Period { get; init; } = null!;

    public int TotalQuantity { get; init; }
}

public sealed class StockMovementReportResponse
{
    public required PagedResult<StockMovementReportItemResponse> Movements { get; init; }

    public required StockMovementTotalsResponse Totals { get; init; }

    public IReadOnlyList<StockMovementPeriodResponse>? Periods { get; init; }
}
