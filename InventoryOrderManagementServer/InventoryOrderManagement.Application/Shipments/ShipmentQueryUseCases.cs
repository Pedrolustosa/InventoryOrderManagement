using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Shipments.Dtos;

namespace InventoryOrderManagement.Application.Shipments;

public sealed class GetAllShipmentsUseCase
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetAllShipmentsUseCase(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<PagedResult<ShipmentSummaryResponse>> ExecuteAsync(
        ShipmentListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.StartDateUtc is { } start && query.EndDateUtc is { } end && start > end)
        {
            throw new InvalidShipmentPeriodException();
        }

        var (page, pageSize) = PageRequest.Normalize(query.Page, query.PageSize);
        var (items, totalItems) = await _shipmentRepository.GetPageAsync(
            new ShipmentQuery
            {
                Status = query.Status,
                OrderId = query.OrderId,
                CustomerId = query.CustomerId,
                CarrierName = query.CarrierName,
                TrackingCode = query.TrackingCode,
                StartDateUtc = query.StartDateUtc,
                EndDateUtc = query.EndDateUtc,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<ShipmentSummaryResponse>.Create(
            items.Select(ShipmentSummaryResponse.FromEntity).ToList(),
            page,
            pageSize,
            totalItems);
    }
}

public sealed class GetShipmentByIdUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;

    public GetShipmentByIdUseCase(IShipmentRepository shipmentRepository, IOrderRepository orderRepository)
    {
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ShipmentResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ShipmentNotFoundException(id);

        var order = await _orderRepository.GetByIdAsync(shipment.OrderId, cancellationToken);
        return ShipmentResponse.FromEntity(shipment, order);
    }
}

public sealed class GetShipmentHistoryUseCase
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetShipmentHistoryUseCase(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<IReadOnlyList<ShipmentStatusHistoryResponse>> ExecuteAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new ShipmentNotFoundException(shipmentId);

        return shipment.History
            .OrderBy(entry => entry.OccurredAtUtc)
            .Select(ShipmentStatusHistoryResponse.FromEntity)
            .ToList();
    }
}
