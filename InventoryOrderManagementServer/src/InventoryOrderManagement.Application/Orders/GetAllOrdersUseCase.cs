using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;

namespace InventoryOrderManagement.Application.Orders;

public sealed class GetAllOrdersUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetAllOrdersUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> ExecuteAsync(
        OrderQuery? query,
        CancellationToken cancellationToken = default)
    {
        var start = NormalizeStart(query?.StartDateUtc);
        var end = NormalizeEnd(query?.EndDateUtc);

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new InvalidOrderPeriodException();
        }

        var orders = await _orderRepository.GetAllAsync(
            new OrderQuery
            {
                CustomerId = query?.CustomerId,
                Status = query?.Status,
                StartDateUtc = start,
                EndDateUtc = end,
                OrderNumber = string.IsNullOrWhiteSpace(query?.OrderNumber) ? null : query.OrderNumber.Trim()
            },
            cancellationToken);

        return orders.Select(OrderSummaryResponse.FromOrder).ToList();
    }

    private static DateTime? NormalizeStart(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        return instant.Kind == DateTimeKind.Utc
            ? instant
            : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
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

        return instant.Kind == DateTimeKind.Utc
            ? instant
            : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }
}
