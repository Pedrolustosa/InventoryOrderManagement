using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Shipments.Dtos;

namespace InventoryOrderManagement.Application.Shipments;

public sealed class UpdateShipmentUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public UpdateShipmentUseCase(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ShipmentResponse> ExecuteAsync(
        Guid shipmentId,
        UpdateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Domain.Entities.Shipment? updated = null;
        Domain.Entities.Order? order = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            shipment.UpdateInformation(request.CarrierName, request.TrackingCode, request.Notes);
            order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct);
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not updated.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentUpdated,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, order);
    }
}
