using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Purchases.Dtos;

public sealed class PurchaseOrderItemResponse
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public required int OrderedQuantity { get; init; }

    public required int ReceivedQuantity { get; init; }

    public required int PendingQuantity { get; init; }

    public required decimal UnitCost { get; init; }

    public required decimal LineTotal { get; init; }

    public static PurchaseOrderItemResponse FromItem(PurchaseOrderItem item)
    {
        return new PurchaseOrderItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductSku = item.ProductSku,
            ProductName = item.ProductName,
            OrderedQuantity = item.OrderedQuantity,
            ReceivedQuantity = item.ReceivedQuantity,
            PendingQuantity = item.PendingQuantity,
            UnitCost = item.UnitCost,
            LineTotal = item.LineTotal
        };
    }
}

public sealed class PurchaseOrderResponse
{
    public required Guid Id { get; init; }

    public required string PurchaseOrderNumber { get; init; }

    public required Guid SupplierId { get; init; }

    public required string SupplierName { get; init; }

    public required PurchaseOrderStatus Status { get; init; }

    public required IReadOnlyList<PurchaseOrderItemResponse> Items { get; init; }

    public required decimal Subtotal { get; init; }

    public string? Notes { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? SentAtUtc { get; init; }

    public DateTime? ReceivedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public string? CancellationReason { get; init; }

    public required int Version { get; init; }

    public static PurchaseOrderResponse FromOrder(PurchaseOrder order)
    {
        return new PurchaseOrderResponse
        {
            Id = order.Id,
            PurchaseOrderNumber = order.PurchaseOrderNumber,
            SupplierId = order.SupplierId,
            SupplierName = order.SupplierName,
            Status = order.Status,
            Items = order.Items.Select(PurchaseOrderItemResponse.FromItem).ToList(),
            Subtotal = order.Subtotal,
            Notes = order.Notes,
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            SentAtUtc = order.SentAtUtc,
            ReceivedAtUtc = order.ReceivedAtUtc,
            CancelledAtUtc = order.CancelledAtUtc,
            CancellationReason = order.CancellationReason,
            Version = order.Version
        };
    }
}

public sealed class PurchaseOrderSummaryResponse
{
    public required Guid Id { get; init; }

    public required string PurchaseOrderNumber { get; init; }

    public required Guid SupplierId { get; init; }

    public required string SupplierName { get; init; }

    public required PurchaseOrderStatus Status { get; init; }

    public required decimal Subtotal { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static PurchaseOrderSummaryResponse FromOrder(PurchaseOrder order)
    {
        return new PurchaseOrderSummaryResponse
        {
            Id = order.Id,
            PurchaseOrderNumber = order.PurchaseOrderNumber,
            SupplierId = order.SupplierId,
            SupplierName = order.SupplierName,
            Status = order.Status,
            Subtotal = order.Subtotal,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }
}
