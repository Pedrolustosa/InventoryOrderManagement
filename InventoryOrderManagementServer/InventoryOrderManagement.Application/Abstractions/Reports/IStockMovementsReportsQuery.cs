using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Reports;

public interface IStockMovementsReportsQuery
{
    Task<PagedResult<StockMovementReportRow>> GetPageAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementTypeQuantity>> GetQuantitiesByTypeAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementInstantQuantity>> GetQuantitiesAsync(
        StockMovementReportFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed class StockMovementReportFilter
{
    public Guid? ProductId { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? OrderId { get; init; }

    public StockMovementType? MovementType { get; init; }

    public DateTime? StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}

public sealed class StockMovementReportRow
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

public sealed class MovementTypeQuantity
{
    public StockMovementType MovementType { get; init; }

    public int Quantity { get; init; }
}

public sealed class MovementInstantQuantity
{
    public DateTime CreatedAtUtc { get; init; }

    public int Quantity { get; init; }
}
