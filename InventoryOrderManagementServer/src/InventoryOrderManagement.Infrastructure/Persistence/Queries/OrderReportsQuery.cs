using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

public sealed class OrderReportsQuery : IOrderReportsQuery
{
    private static readonly OrderStatus[] CommercialStatuses =
    [
        OrderStatus.Confirmed,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    ];

    private readonly InventoryDbContext _dbContext;

    public OrderReportsQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderStatusCounts> GetStatusCountsAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = Scoped(customerId);

        return new OrderStatusCounts
        {
            TotalOrdersCreated = await CountCreatedAsync(orders, startUtc, endUtc, cancellationToken),
            DraftOrders = await CountCreatedAsync(
                orders.Where(order => order.Status == OrderStatus.Draft),
                startUtc,
                endUtc,
                cancellationToken),
            ConfirmedOrders = await CountConfirmedAsync(
                orders.Where(order => order.Status == OrderStatus.Confirmed),
                startUtc,
                endUtc,
                cancellationToken),
            ShippedOrders = await CountShippedAsync(
                orders.Where(order => order.Status == OrderStatus.Shipped),
                startUtc,
                endUtc,
                cancellationToken),
            DeliveredOrders = await CountDeliveredAsync(
                orders.Where(order => order.Status == OrderStatus.Delivered),
                startUtc,
                endUtc,
                cancellationToken),
            CancelledOrders = await CountCancelledAsync(
                orders.Where(order => order.Status == OrderStatus.Cancelled),
                startUtc,
                endUtc,
                cancellationToken)
        };
    }

    public async Task<IReadOnlyList<CommercialOrderAmount>> GetCommercialOrdersAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        return await Commercial(customerId, startUtc, endUtc)
            .Select(order => new CommercialOrderAmount
            {
                Subtotal = order.Subtotal,
                ConfirmedAtUtc = order.ConfirmedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CancellationCohortCounts> GetCancellationCohortAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var created = Created(Scoped(customerId), startUtc, endUtc);

        return new CancellationCohortCounts
        {
            OrdersCreated = await created.CountAsync(cancellationToken),
            CreatedAndCancelled = await created.CountAsync(
                order => order.Status == OrderStatus.Cancelled,
                cancellationToken)
        };
    }

    public async Task<IReadOnlyList<CancelledOrderAmount>> GetCancelledOrdersAsync(
        Guid? customerId,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        return await Cancelled(customerId, startUtc, endUtc)
            .Select(order => new CancelledOrderAmount
            {
                Subtotal = order.Subtotal,
                CancelledAtUtc = order.CancelledAtUtc!.Value
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Domain.Entities.Order> Scoped(Guid? customerId)
    {
        var orders = _dbContext.Orders.AsNoTracking();
        if (customerId.HasValue)
        {
            orders = orders.Where(order => order.CustomerId == customerId.Value);
        }

        return orders;
    }

    private IQueryable<Domain.Entities.Order> Commercial(Guid? customerId, DateTime? startUtc, DateTime? endUtc)
    {
        var orders = Scoped(customerId)
            .Where(order => CommercialStatuses.Contains(order.Status) && order.ConfirmedAtUtc != null);

        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.ConfirmedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.ConfirmedAtUtc < endUtc.Value);
        }

        return orders;
    }

    private IQueryable<Domain.Entities.Order> Cancelled(Guid? customerId, DateTime? startUtc, DateTime? endUtc)
    {
        var orders = Scoped(customerId)
            .Where(order => order.Status == OrderStatus.Cancelled && order.CancelledAtUtc != null);

        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.CancelledAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.CancelledAtUtc < endUtc.Value);
        }

        return orders;
    }

    private static IQueryable<Domain.Entities.Order> Created(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc)
    {
        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAtUtc < endUtc.Value);
        }

        return orders;
    }

    private static Task<int> CountCreatedAsync(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        return Created(orders, startUtc, endUtc).CountAsync(cancellationToken);
    }

    private static Task<int> CountConfirmedAsync(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        orders = orders.Where(order => order.ConfirmedAtUtc != null);
        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.ConfirmedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.ConfirmedAtUtc < endUtc.Value);
        }

        return orders.CountAsync(cancellationToken);
    }

    private static Task<int> CountShippedAsync(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        orders = orders.Where(order => order.ShippedAtUtc != null);
        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.ShippedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.ShippedAtUtc < endUtc.Value);
        }

        return orders.CountAsync(cancellationToken);
    }

    private static Task<int> CountDeliveredAsync(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        orders = orders.Where(order => order.DeliveredAtUtc != null);
        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.DeliveredAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.DeliveredAtUtc < endUtc.Value);
        }

        return orders.CountAsync(cancellationToken);
    }

    private static Task<int> CountCancelledAsync(
        IQueryable<Domain.Entities.Order> orders,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        orders = orders.Where(order => order.CancelledAtUtc != null);
        if (startUtc.HasValue)
        {
            orders = orders.Where(order => order.CancelledAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            orders = orders.Where(order => order.CancelledAtUtc < endUtc.Value);
        }

        return orders.CountAsync(cancellationToken);
    }
}
