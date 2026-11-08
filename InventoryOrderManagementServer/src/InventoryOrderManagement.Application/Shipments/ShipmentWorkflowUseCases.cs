using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Shipments;

public sealed class DispatchShipmentUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public DispatchShipmentUseCase(
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
        ShipmentNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? updated = null;
        Order? order = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            order = await _orderRepository.GetTrackedByIdAsync(shipment.OrderId, ct)
                ?? throw new OrderNotFoundException(shipment.OrderId);

            EnsureOrderCompatibleWithDispatch(order);

            var dispatchedItems = order.Items
                .Select(item => new Domain.Events.ShipmentDispatchedItem(item.ProductId, item.Quantity))
                .ToList();

            shipment.Dispatch(request.Notes, _currentUser.UserId, dispatchedItems);
            order.Ship();
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not dispatched.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentDispatched,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, order);
    }

    private static void EnsureOrderCompatibleWithDispatch(Order order)
    {
        if (order.Status != Domain.Enums.OrderStatus.Confirmed)
        {
            throw new ShipmentConflictException(
                $"The sales order status '{order.Status}' is incompatible with shipment dispatch.");
        }
    }
}

public sealed class SetShipmentInTransitUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public SetShipmentInTransitUseCase(
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
        ShipmentNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? updated = null;
        Order? order = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct)
                ?? throw new OrderNotFoundException(shipment.OrderId);

            EnsureOrderShipped(order);
            shipment.SetInTransit(request.Notes, _currentUser.UserId);
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not updated.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentInTransit,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, order);
    }

    private static void EnsureOrderShipped(Order order)
    {
        if (order.Status != Domain.Enums.OrderStatus.Shipped)
        {
            throw new ShipmentConflictException(
                $"The sales order status '{order.Status}' is incompatible with this shipment transition.");
        }
    }
}

public sealed class MarkShipmentDeliveredUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public MarkShipmentDeliveredUseCase(
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
        ShipmentNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? updated = null;
        Order? order = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            order = await _orderRepository.GetTrackedByIdAsync(shipment.OrderId, ct)
                ?? throw new OrderNotFoundException(shipment.OrderId);

            if (order.Status != Domain.Enums.OrderStatus.Shipped)
            {
                throw new ShipmentConflictException(
                    $"The sales order status '{order.Status}' is incompatible with delivery confirmation.");
            }

            shipment.MarkAsDelivered(request.Notes, _currentUser.UserId);
            order.Deliver();
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not delivered.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentDelivered,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, order);
    }
}

public sealed class MarkShipmentFailedUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<MarkShipmentFailedUseCase> _logger;

    public MarkShipmentFailedUseCase(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser,
        INotificationDispatcher notificationDispatcher,
        ILogger<MarkShipmentFailedUseCase> logger)
    {
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<ShipmentResponse> ExecuteAsync(
        Guid shipmentId,
        FailShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? updated = null;
        Order? order = null;
        Guid? historyId = null;
        DateTime failedAtUtc = DateTime.UtcNow;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct)
                ?? throw new OrderNotFoundException(shipment.OrderId);

            if (order.Status != Domain.Enums.OrderStatus.Shipped)
            {
                throw new ShipmentConflictException(
                    $"The sales order status '{order.Status}' is incompatible with a failed delivery.");
            }

            shipment.MarkAsFailed(request.FailureReason, request.Notes, _currentUser.UserId);
            var newestHistory = shipment.History.OrderByDescending(entry => entry.OccurredAtUtc).First();
            historyId = newestHistory.Id;
            failedAtUtc = newestHistory.OccurredAtUtc;
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not marked as failed.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentFailed,
            entity.Id,
            cancellationToken,
            request.FailureReason);

        if (historyId is { } shipmentStatusHistoryId)
        {
            await NotificationDispatch.TryAsync(
                ct => _notificationDispatcher.NotifyShipmentFailedAsync(
                    entity.Id,
                    order?.OrderNumber ?? entity.OrderId.ToString(),
                    shipmentStatusHistoryId,
                    request.FailureReason,
                    failedAtUtc,
                    ct),
                _logger,
                $"Failed to dispatch shipment-failed notification for shipment {entity.Id}.",
                cancellationToken);
        }

        return ShipmentResponse.FromEntity(entity, order);
    }
}

public sealed class ResumeShipmentUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public ResumeShipmentUseCase(
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
        ShipmentNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shipment? updated = null;
        Order? order = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var shipment = await _shipmentRepository.GetTrackedByIdAsync(shipmentId, ct)
                ?? throw new ShipmentNotFoundException(shipmentId);

            order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct)
                ?? throw new OrderNotFoundException(shipment.OrderId);

            if (order.Status != Domain.Enums.OrderStatus.Shipped)
            {
                throw new ShipmentConflictException(
                    $"The sales order status '{order.Status}' is incompatible with resuming delivery.");
            }

            shipment.ResumeDelivery(request.Notes, _currentUser.UserId);
            updated = shipment;
        }, cancellationToken);

        var entity = updated ?? throw new InvalidOperationException("Shipment was not resumed.");
        await ShipmentAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ShipmentResumed,
            entity.Id,
            cancellationToken);

        return ShipmentResponse.FromEntity(entity, order);
    }
}
