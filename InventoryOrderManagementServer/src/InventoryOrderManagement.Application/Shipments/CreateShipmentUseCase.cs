using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Shipments;

public sealed class CreateShipmentUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CreateShipmentUseCase(
        IOrderRepository orderRepository,
        IShipmentRepository shipmentRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ShipmentResponse> ExecuteAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? created = null;
        Order? orderSnapshot = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            if (order.Status != OrderStatus.Confirmed)
            {
                throw new ShipmentConflictException("Only confirmed sales orders can create a shipment.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new ShipmentConflictException("Cancelled orders cannot create a shipment.");
            }

            if (await _shipmentRepository.ExistsForOrderAsync(order.Id, ct))
            {
                throw new ShipmentConflictException("This sales order already has a shipment.");
            }

            
            _ = order.ShippingAddress;

            var shipment = Shipment.Create(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.CustomerName,
                request.CarrierName,
                request.TrackingCode,
                request.Notes,
                _currentUser.UserId);

            await _shipmentRepository.AddAsync(shipment, ct);
            created = shipment;
            orderSnapshot = order;
        }, cancellationToken);

        var entity = created ?? throw new InvalidOperationException("Shipment was not created.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentCreated,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, orderSnapshot);
    }
}
