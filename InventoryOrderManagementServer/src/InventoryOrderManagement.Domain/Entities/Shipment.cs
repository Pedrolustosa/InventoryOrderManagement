using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

public class Shipment : AggregateRoot
{
    public const int CarrierNameMaxLength = 200;
    public const int TrackingCodeMaxLength = 80;
    public const int NotesMaxLength = 500;
    public const int FailureReasonMaxLength = 300;
    public const int OrderNumberMaxLength = 64;
    public const int CustomerNameMaxLength = 200;

    private readonly List<ShipmentStatusHistory> _history = [];

    private Shipment()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string OrderNumber { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    public string CustomerName { get; private set; } = null!;

    public ShipmentStatus Status { get; private set; }

    public string? CarrierName { get; private set; }

    public string? TrackingCode { get; private set; }

    public string? Notes { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? ShippedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<ShipmentStatusHistory> History => _history;

    public static Shipment Create(
        Guid orderId,
        string orderNumber,
        Guid customerId,
        string customerName,
        string? carrierName,
        string? trackingCode,
        string? notes,
        Guid? createdByUserId)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Sales order is required.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer is required.");
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderNumber = NormalizeRequired(orderNumber, OrderNumberMaxLength, "Order number"),
            CustomerId = customerId,
            CustomerName = NormalizeRequired(customerName, CustomerNameMaxLength, "Customer name"),
            Status = ShipmentStatus.Preparing,
            CarrierName = NormalizeOptional(carrierName, CarrierNameMaxLength, "Carrier name"),
            TrackingCode = NormalizeOptional(trackingCode, TrackingCodeMaxLength, "Tracking code"),
            Notes = NormalizeOptional(notes, NotesMaxLength, "Notes"),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            Version = 0
        };

        shipment.AppendHistory(null, ShipmentStatus.Preparing, notes, createdByUserId);
        return shipment;
    }

    public void UpdateInformation(string? carrierName, string? trackingCode, string? notes)
    {
        EnsureStatus(ShipmentStatus.Preparing);

        CarrierName = NormalizeOptional(carrierName, CarrierNameMaxLength, "Carrier name");
        TrackingCode = NormalizeOptional(trackingCode, TrackingCodeMaxLength, "Tracking code");
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes");
        Touch();
    }

    public void Dispatch(string? notes, Guid? changedByUserId)
    {
        EnsureStatus(ShipmentStatus.Preparing);

        if (string.IsNullOrWhiteSpace(CarrierName))
        {
            throw new DomainException("Carrier name is required before dispatch.");
        }

        var previous = Status;
        Status = ShipmentStatus.Shipped;
        ShippedAtUtc = DateTime.UtcNow;
        FailureReason = null;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes") ?? Notes;
        AppendHistory(previous, Status, notes, changedByUserId);
        Touch();
    }

    public void SetInTransit(string? notes, Guid? changedByUserId)
    {
        EnsureStatus(ShipmentStatus.Shipped);

        var previous = Status;
        Status = ShipmentStatus.InTransit;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes") ?? Notes;
        AppendHistory(previous, Status, notes, changedByUserId);
        Touch();
    }

    public void MarkAsDelivered(string? notes, Guid? changedByUserId)
    {
        if (Status is not (ShipmentStatus.Shipped or ShipmentStatus.InTransit))
        {
            throw new InvalidShipmentStatusException(Status);
        }

        var previous = Status;
        Status = ShipmentStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        FailureReason = null;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes") ?? Notes;
        AppendHistory(previous, Status, notes, changedByUserId);
        Touch();
    }

    public void MarkAsFailed(string failureReason, string? notes, Guid? changedByUserId)
    {
        if (Status is not (ShipmentStatus.Shipped or ShipmentStatus.InTransit))
        {
            throw new InvalidShipmentStatusException(Status);
        }

        var normalizedReason = NormalizeRequired(failureReason, FailureReasonMaxLength, "Failure reason");
        var previous = Status;
        Status = ShipmentStatus.Failed;
        FailureReason = normalizedReason;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes") ?? Notes;
        AppendHistory(previous, Status, notes ?? FailureReason, changedByUserId);
        Touch();
    }

    public void ResumeDelivery(string? notes, Guid? changedByUserId)
    {
        EnsureStatus(ShipmentStatus.Failed);

        var previous = Status;
        Status = ShipmentStatus.InTransit;
        FailureReason = null;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes") ?? Notes;
        AppendHistory(previous, Status, notes, changedByUserId);
        Touch();
    }

    private void AppendHistory(
        ShipmentStatus? previousStatus,
        ShipmentStatus newStatus,
        string? notes,
        Guid? changedByUserId)
    {
        _history.Add(ShipmentStatusHistory.Create(
            Id,
            previousStatus,
            newStatus,
            NormalizeOptional(notes, ShipmentStatusHistory.NotesMaxLength, "Notes"),
            changedByUserId));
    }

    private void EnsureStatus(ShipmentStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidShipmentStatusException(Status);
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
