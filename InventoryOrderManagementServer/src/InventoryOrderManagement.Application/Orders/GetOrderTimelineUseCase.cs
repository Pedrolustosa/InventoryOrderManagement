using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Orders;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Security;

namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Builds the timeline of a sales order. Finance entries require <see cref="Permissions.Finance.View"/> and
/// return entries <see cref="Permissions.Returns.View"/>; without them those sources are not even loaded.
/// Order, stock and shipment entries are visible to anyone allowed to view the order.
/// </summary>
public sealed class GetOrderTimelineUseCase
{
    private readonly IOrderTimelineQuery _timelineQuery;
    private readonly ICurrentUser _currentUser;

    public GetOrderTimelineUseCase(IOrderTimelineQuery timelineQuery, ICurrentUser currentUser)
    {
        _timelineQuery = timelineQuery;
        _currentUser = currentUser;
    }

    public async Task<OrderTimelineDto> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var canViewFinance = _currentUser.Permissions.Contains(Permissions.Finance.View);
        var canViewReturns = _currentUser.Permissions.Contains(Permissions.Returns.View);

        var sources = await _timelineQuery.LoadAsync(orderId, canViewFinance, canViewReturns, cancellationToken)
            ?? throw new OrderNotFoundException(orderId);

        var timeline = OrderTimelineBuilder.Build(sources);

        // Defense in depth: never leak a module the user cannot see, whatever the query returned.
        return new OrderTimelineDto
        {
            OrderId = timeline.OrderId,
            OrderNumber = timeline.OrderNumber,
            CurrentStatus = timeline.CurrentStatus,
            Entries = timeline.Entries
                .Where(entry => entry.Module switch
                {
                    OrderTimelineModule.Finance => canViewFinance,
                    OrderTimelineModule.Return => canViewReturns,
                    _ => true
                })
                .ToList()
        };
    }
}
