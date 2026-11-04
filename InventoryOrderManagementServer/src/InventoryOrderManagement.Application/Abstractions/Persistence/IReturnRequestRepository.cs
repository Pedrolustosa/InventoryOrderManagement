using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IReturnRequestRepository
{
    Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);

    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReturnRequest?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(string returnNumber, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ReturnRequest> Items, int TotalItems)> GetPageAsync(
        ReturnRequestQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class ReturnRequestQuery
{
    public ReturnType? Type { get; init; }

    public ReturnStatus? Status { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? PurchaseOrderId { get; init; }

    public string? ReturnNumber { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
