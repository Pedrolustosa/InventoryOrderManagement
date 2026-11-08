using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

public sealed class DashboardQuery : IDashboardQuery
{
    private static readonly PurchaseOrderStatus[] CommercialPurchaseStatuses =
    [
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.Received
    ];

    private readonly InventoryDbContext _dbContext;

    public DashboardQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardPartyCounts> GetPartyCountsAsync(CancellationToken cancellationToken = default)
    {
        var customers = _dbContext.Customers.AsNoTracking();
        var suppliers = _dbContext.Suppliers.AsNoTracking();

        return new DashboardPartyCounts
        {
            CustomersTotal = await customers.CountAsync(cancellationToken),
            CustomersActive = await customers.CountAsync(customer => customer.IsActive, cancellationToken),
            SuppliersTotal = await suppliers.CountAsync(cancellationToken),
            SuppliersActive = await suppliers.CountAsync(supplier => supplier.IsActive, cancellationToken)
        };
    }

    public async Task<DashboardPurchaseStatusCounts> GetPurchaseStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = _dbContext.PurchaseOrders.AsNoTracking();

        return new DashboardPurchaseStatusCounts
        {
            Total = await orders.CountAsync(cancellationToken),
            Draft = await orders.CountAsync(order => order.Status == PurchaseOrderStatus.Draft, cancellationToken),
            Sent = await orders.CountAsync(order => order.Status == PurchaseOrderStatus.Sent, cancellationToken),
            PartiallyReceived = await orders.CountAsync(
                order => order.Status == PurchaseOrderStatus.PartiallyReceived,
                cancellationToken),
            Received = await orders.CountAsync(order => order.Status == PurchaseOrderStatus.Received, cancellationToken),
            Cancelled = await orders.CountAsync(order => order.Status == PurchaseOrderStatus.Cancelled, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<DashboardPurchaseCommercialAmount>> GetPurchaseCommercialAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(order => CommercialPurchaseStatuses.Contains(order.Status) && order.SentAtUtc != null);

        if (startUtc.HasValue)
        {
            query = query.Where(order => order.SentAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(order => order.SentAtUtc < endUtc.Value);
        }

        return await query
            .Select(order => new DashboardPurchaseCommercialAmount
            {
                Subtotal = order.Subtotal,
                SentAtUtc = order.SentAtUtc!.Value
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardShipmentStatusCounts> GetShipmentStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var shipments = _dbContext.Shipments.AsNoTracking();

        return new DashboardShipmentStatusCounts
        {
            Preparing = await shipments.CountAsync(
                shipment => shipment.Status == ShipmentStatus.Preparing,
                cancellationToken),
            Shipped = await shipments.CountAsync(
                shipment => shipment.Status == ShipmentStatus.Shipped,
                cancellationToken),
            InTransit = await shipments.CountAsync(
                shipment => shipment.Status == ShipmentStatus.InTransit,
                cancellationToken),
            Delivered = await shipments.CountAsync(
                shipment => shipment.Status == ShipmentStatus.Delivered,
                cancellationToken),
            Failed = await shipments.CountAsync(
                shipment => shipment.Status == ShipmentStatus.Failed,
                cancellationToken)
        };
    }

    public async Task<DashboardReturnStatusCounts> GetReturnStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var returns = _dbContext.ReturnRequests.AsNoTracking();

        return new DashboardReturnStatusCounts
        {
            Draft = await returns.CountAsync(item => item.Status == ReturnStatus.Draft, cancellationToken),
            Authorized = await returns.CountAsync(item => item.Status == ReturnStatus.Authorized, cancellationToken),
            Received = await returns.CountAsync(item => item.Status == ReturnStatus.Received, cancellationToken),
            UnderInspection = await returns.CountAsync(
                item => item.Status == ReturnStatus.UnderInspection,
                cancellationToken),
            Completed = await returns.CountAsync(item => item.Status == ReturnStatus.Completed, cancellationToken),
            Dispatched = await returns.CountAsync(item => item.Status == ReturnStatus.Dispatched, cancellationToken),
            Rejected = await returns.CountAsync(item => item.Status == ReturnStatus.Rejected, cancellationToken),
            Cancelled = await returns.CountAsync(item => item.Status == ReturnStatus.Cancelled, cancellationToken)
        };
    }

    public async Task<DashboardFinanceOpenMetrics> GetFinanceOpenMetricsAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from installment in _dbContext.FinancialInstallments.AsNoTracking()
                join title in _dbContext.FinancialTitles.AsNoTracking()
                    on installment.FinancialTitleId equals title.Id
                select new
                {
                    title.Type,
                    TitleCancelled = title.CancelledAtUtc != null,
                    installment.DueDate,
                    installment.SettledPrincipalAmount,
                    installment.OutstandingBalance
                })
            .ToListAsync(cancellationToken);

        var payablesPendingCount = 0;
        var receivablesPendingCount = 0;
        var overduePayablesCount = 0;
        var overdueReceivablesCount = 0;
        decimal payablesPendingAmount = 0;
        decimal receivablesPendingAmount = 0;
        decimal overduePayablesAmount = 0;
        decimal overdueReceivablesAmount = 0;

        foreach (var row in rows)
        {
            if (row.TitleCancelled || row.OutstandingBalance <= 0)
            {
                continue;
            }

            var status = FinancialStatus.Installment(
                row.OutstandingBalance,
                row.SettledPrincipalAmount,
                row.DueDate,
                today,
                row.TitleCancelled);

            var isPayable = row.Type == FinancialTitleType.Payable;
            if (status == FinancialInstallmentStatus.Overdue)
            {
                if (isPayable)
                {
                    overduePayablesCount++;
                    overduePayablesAmount += row.OutstandingBalance;
                }
                else
                {
                    overdueReceivablesCount++;
                    overdueReceivablesAmount += row.OutstandingBalance;
                }
            }
            else
            {
                if (isPayable)
                {
                    payablesPendingCount++;
                    payablesPendingAmount += row.OutstandingBalance;
                }
                else
                {
                    receivablesPendingCount++;
                    receivablesPendingAmount += row.OutstandingBalance;
                }
            }
        }

        return new DashboardFinanceOpenMetrics
        {
            PayablesPendingCount = payablesPendingCount,
            ReceivablesPendingCount = receivablesPendingCount,
            OverduePayablesCount = overduePayablesCount,
            OverdueReceivablesCount = overdueReceivablesCount,
            PayablesPendingAmount = decimal.Round(payablesPendingAmount, 2, MidpointRounding.AwayFromZero),
            ReceivablesPendingAmount = decimal.Round(receivablesPendingAmount, 2, MidpointRounding.AwayFromZero),
            OverduePayablesAmount = decimal.Round(overduePayablesAmount, 2, MidpointRounding.AwayFromZero),
            OverdueReceivablesAmount = decimal.Round(overdueReceivablesAmount, 2, MidpointRounding.AwayFromZero)
        };
    }

    public async Task<IReadOnlyList<DashboardSalesTrendPoint>> GetSalesTrendPointsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var commercialStatuses = new[]
        {
            OrderStatus.Confirmed,
            OrderStatus.Shipped,
            OrderStatus.Delivered
        };

        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(order => commercialStatuses.Contains(order.Status) && order.ConfirmedAtUtc != null);

        if (startUtc.HasValue)
        {
            query = query.Where(order => order.ConfirmedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(order => order.ConfirmedAtUtc < endUtc.Value);
        }

        return await query
            .Select(order => new DashboardSalesTrendPoint
            {
                Subtotal = order.Subtotal,
                ConfirmedAtUtc = order.ConfirmedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardPurchaseTrendPoint>> GetPurchaseTrendPointsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var points = await GetPurchaseCommercialAsync(startUtc, endUtc, cancellationToken);
        return points
            .Select(point => new DashboardPurchaseTrendPoint
            {
                Subtotal = point.Subtotal,
                SentAtUtc = point.SentAtUtc
            })
            .ToList();
    }

    public async Task<IReadOnlyList<DashboardRecentOrderRow>> GetRecentOrdersAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(take)
            .Select(order => new DashboardRecentOrderRow
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                Status = order.Status,
                Subtotal = order.Subtotal,
                CreatedAtUtc = order.CreatedAtUtc,
                ConfirmedAtUtc = order.ConfirmedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
