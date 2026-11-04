using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class GetAllPurchaseOrdersUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public GetAllPurchaseOrdersUseCase(IPurchaseOrderRepository purchaseOrderRepository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    public async Task<PagedResult<PurchaseOrderSummaryResponse>> ExecuteAsync(
        PurchaseOrderQuery? query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PageRequest.Normalize(query?.Page, query?.PageSize);
        var start = NormalizeStart(query?.StartDateUtc);
        var end = NormalizeEnd(query?.EndDateUtc);

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new InvalidPurchasePeriodException();
        }

        var (items, totalItems) = await _purchaseOrderRepository.GetPageAsync(
            new PurchaseOrderQuery
            {
                Status = query?.Status,
                SupplierId = query?.SupplierId,
                StartDateUtc = start,
                EndDateUtc = end,
                PurchaseOrderNumber = string.IsNullOrWhiteSpace(query?.PurchaseOrderNumber) ? null : query.PurchaseOrderNumber.Trim(),
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<PurchaseOrderSummaryResponse>.Create(
            items.Select(PurchaseOrderSummaryResponse.FromOrder).ToList(),
            page,
            pageSize,
            totalItems);
    }

    private static DateTime? NormalizeStart(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        return instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeEnd(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        if (instant.TimeOfDay == TimeSpan.Zero)
        {
            instant = instant.Date.AddDays(1).AddTicks(-1);
        }

        return instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }
}
