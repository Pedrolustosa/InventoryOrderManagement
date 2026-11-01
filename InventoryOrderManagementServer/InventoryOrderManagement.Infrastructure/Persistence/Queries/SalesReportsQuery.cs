using InventoryOrderManagement.Application.Abstractions.Reports;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Queries;

public sealed class SalesReportsQuery : ISalesReportsQuery
{
    private static readonly OrderStatus[] CommercialStatuses =
    [
        OrderStatus.Confirmed,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    ];

    private readonly InventoryDbContext _dbContext;

    public SalesReportsQuery(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SoldItemRow>> GetSoldItemsAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = Commercial(startUtc, endUtc);

        return await
            (from order in orders
             join item in _dbContext.OrderItems.AsNoTracking()
                 on order.Id equals item.OrderId
             select new SoldItemRow
             {
                 ProductId = item.ProductId,
                 ProductSku = item.ProductSku,
                 ProductName = item.ProductName,
                 Quantity = item.Quantity,
                 LineTotal = item.LineTotal,
                 OrderId = order.Id,
                 ConfirmedAtUtc = order.ConfirmedAtUtc!.Value
             })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSaleRow>> GetCustomerSalesAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = Commercial(startUtc, endUtc);

        var rows = await
            (from order in orders
             join item in _dbContext.OrderItems.AsNoTracking()
                 on order.Id equals item.OrderId into items
             select new
             {
                 order.CustomerId,
                 order.CustomerName,
                 order.Subtotal,
                 ConfirmedAtUtc = order.ConfirmedAtUtc!.Value,
                 ItemQuantity = items.Sum(item => (int?)item.Quantity) ?? 0
             })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CustomerSaleRow
            {
                CustomerId = row.CustomerId,
                CustomerName = row.CustomerName,
                Subtotal = row.Subtotal,
                ConfirmedAtUtc = row.ConfirmedAtUtc,
                ItemQuantity = row.ItemQuantity
            })
            .ToList();
    }

    private IQueryable<Domain.Entities.Order> Commercial(DateTime? startUtc, DateTime? endUtc)
    {
        var orders = _dbContext.Orders.AsNoTracking()
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
}
