using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Shipments.Dtos;

public sealed class CreateShipmentRequest
{
    public Guid OrderId { get; init; }

    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }
}

public sealed class UpdateShipmentRequest
{
    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }
}

public sealed class ShipmentNotesRequest
{
    public string? Notes { get; init; }
}

public sealed class FailShipmentRequest
{
    public string FailureReason { get; init; } = string.Empty;

    public string? Notes { get; init; }
}

public sealed class ShipmentListQuery
{
    public ShipmentStatus? Status { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? CustomerId { get; init; }

    public string? CarrierName { get; init; }

    public string? TrackingCode { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
