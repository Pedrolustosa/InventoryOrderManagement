using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Purchases.Dtos;

public sealed class PurchaseReceiptItemResponse
{
    public required Guid Id { get; init; }

    public required Guid PurchaseOrderItemId { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public required int QuantityReceived { get; init; }

    public required decimal UnitCostSnapshot { get; init; }
}

public sealed class PurchaseReceiptResponse
{
    public required Guid Id { get; init; }

    public required string ReceiptNumber { get; init; }

    public required Guid PurchaseOrderId { get; init; }

    public required DateTime ReceivedAtUtc { get; init; }

    public string? Notes { get; init; }

    public required IReadOnlyList<PurchaseReceiptItemResponse> Items { get; init; }

    public static PurchaseReceiptResponse FromReceipt(PurchaseReceipt receipt)
    {
        return new PurchaseReceiptResponse
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            Notes = receipt.Notes,
            Items = receipt.Items.Select(item => new PurchaseReceiptItemResponse
            {
                Id = item.Id,
                PurchaseOrderItemId = item.PurchaseOrderItemId,
                ProductId = item.ProductId,
                ProductSku = item.ProductSku,
                ProductName = item.ProductName,
                QuantityReceived = item.QuantityReceived,
                UnitCostSnapshot = item.UnitCostSnapshot
            }).ToList()
        };
    }
}
