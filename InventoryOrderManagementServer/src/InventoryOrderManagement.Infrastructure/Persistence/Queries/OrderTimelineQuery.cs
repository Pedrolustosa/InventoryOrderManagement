using InventoryOrderManagement.Application.Abstractions.Orders;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

/// <summary>
/// Loads every timeline source with no-tracking projections and a fixed number of queries, whatever the size
/// of the timeline: order, shipment history, stock movements, returns, titles, settlements and user names
/// (at most 7). Finance and return queries run only when requested.
/// </summary>
public sealed class OrderTimelineQuery : IOrderTimelineQuery
{
    private readonly InventoryDbContext _dbContext;

    public OrderTimelineQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderTimelineSources?> LoadAsync(
        Guid orderId,
        bool includeFinance,
        bool includeReturns,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders.AsNoTracking()
            .Where(entity => entity.Id == orderId)
            .Select(entity => new OrderTimelineOrderRow(
                entity.Id,
                entity.OrderNumber,
                entity.Status,
                entity.Subtotal,
                entity.CreatedAtUtc,
                entity.ConfirmedAtUtc,
                entity.ShippedAtUtc,
                entity.DeliveredAtUtc,
                entity.CancelledAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return null;
        }

        var shipmentHistory = await (
                from history in _dbContext.ShipmentStatusHistories.AsNoTracking()
                join shipment in _dbContext.Shipments.AsNoTracking() on history.ShipmentId equals shipment.Id
                where shipment.OrderId == orderId
                select new OrderTimelineShipmentHistoryRow(
                    shipment.Id,
                    history.Id,
                    shipment.CarrierName,
                    shipment.TrackingCode,
                    history.PreviousStatus,
                    history.NewStatus,
                    history.OccurredAtUtc,
                    history.ChangedByUserId))
            .ToListAsync(cancellationToken);

        var movements = await (
                from movement in _dbContext.StockMovements.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking() on movement.ProductId equals product.Id
                where movement.OrderId == orderId
                select new OrderTimelineMovementRow(
                    movement.Id,
                    movement.ProductId,
                    product.Sku,
                    product.Name,
                    movement.MovementType,
                    movement.Quantity,
                    movement.ShipmentId,
                    movement.ReturnRequestId,
                    movement.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        // Returns are needed for the return entries and to find the titles a return generated.
        var returns = includeFinance || includeReturns
            ? await _dbContext.ReturnRequests.AsNoTracking()
                .Where(entity => entity.OrderId == orderId)
                .Select(entity => new OrderTimelineReturnRow(
                    entity.Id,
                    entity.ReturnNumber,
                    entity.Type,
                    entity.Status,
                    entity.EstimatedAmount,
                    entity.CreatedAtUtc,
                    entity.ReceivedAtUtc,
                    entity.CompletedAtUtc,
                    entity.CancelledAtUtc,
                    entity.CreatedByUserId))
                .ToListAsync(cancellationToken)
            : [];

        List<OrderTimelineTitleRow> titles = [];
        List<OrderTimelineSettlementRow> settlements = [];
        if (includeFinance)
        {
            var returnIds = returns.Select(row => (Guid?)row.Id).ToList();
            titles = await _dbContext.FinancialTitles.AsNoTracking()
                .Where(title =>
                    (title.SourceType == FinancialSourceType.SalesOrder && title.SourceId == orderId)
                    || (title.SourceType == FinancialSourceType.Return && returnIds.Contains(title.SourceId)))
                .Select(title => new OrderTimelineTitleRow(
                    title.Id,
                    title.TitleNumber,
                    title.Type,
                    title.SourceType,
                    title.SourceId,
                    title.TotalAmount,
                    title.Installments
                        .OrderBy(installment => installment.DueDate)
                        .Select(installment => (DateOnly?)installment.DueDate)
                        .FirstOrDefault(),
                    title.CreatedAtUtc,
                    title.CancelledAtUtc,
                    title.CreatedByUserId))
                .ToListAsync(cancellationToken);

            var titleIds = titles.Select(title => title.Id).ToList();
            settlements = titleIds.Count == 0
                ? []
                : await _dbContext.FinancialSettlements.AsNoTracking()
                    .Where(settlement => titleIds.Contains(settlement.FinancialTitleId))
                    .Select(settlement => new OrderTimelineSettlementRow(
                        settlement.Id,
                        settlement.FinancialTitleId,
                        settlement.PaymentMethod,
                        settlement.SettledPrincipalAmount,
                        settlement.OccurredAtUtc,
                        settlement.CreatedByUserId))
                    .ToListAsync(cancellationToken);
        }

        var visibleReturns = includeReturns ? returns : [];
        var userIds = shipmentHistory.Select(row => row.ChangedByUserId)
            .Concat(visibleReturns.Select(row => row.CreatedByUserId))
            .Concat(titles.Select(row => row.CreatedByUserId))
            .Concat(settlements.Select(row => row.CreatedByUserId))
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var userNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Users.AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.DisplayName })
                .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        return new OrderTimelineSources
        {
            Order = order,
            ShipmentHistory = shipmentHistory,
            Movements = movements,
            Returns = visibleReturns,
            Titles = titles,
            Settlements = settlements,
            UserNames = userNames
        };
    }
}
