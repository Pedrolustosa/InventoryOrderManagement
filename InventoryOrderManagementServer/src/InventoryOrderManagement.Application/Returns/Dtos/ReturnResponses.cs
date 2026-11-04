using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Returns.Dtos;

public sealed class ReturnItemResponse
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public Guid? OrderItemId { get; init; }

    public Guid? PurchaseOrderItemId { get; init; }

    public required int SourceQuantity { get; init; }

    public required int PreviouslyReturnedQuantity { get; init; }

    public required int RequestedQuantity { get; init; }

    public required int ReceivedQuantity { get; init; }

    public required int DispatchedQuantity { get; init; }

    public required int RestockedQuantity { get; init; }

    public required int QuarantinedQuantity { get; init; }

    public required int DiscardedQuantity { get; init; }

    public required int RejectedQuantity { get; init; }

    public required decimal UnitValueSnapshot { get; init; }

    public required decimal LineEstimatedAmount { get; init; }

    public ReturnItemCondition? Condition { get; init; }

    public string? InspectionNotes { get; init; }

    public DateTime? InspectedAtUtc { get; init; }

    public Guid? InspectedByUserId { get; init; }

    public static ReturnItemResponse FromItem(ReturnItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductSku = item.ProductSku,
        ProductName = item.ProductName,
        OrderItemId = item.OrderItemId,
        PurchaseOrderItemId = item.PurchaseOrderItemId,
        SourceQuantity = item.SourceQuantity,
        PreviouslyReturnedQuantity = item.PreviouslyReturnedQuantity,
        RequestedQuantity = item.RequestedQuantity,
        ReceivedQuantity = item.ReceivedQuantity,
        DispatchedQuantity = item.DispatchedQuantity,
        RestockedQuantity = item.RestockedQuantity,
        QuarantinedQuantity = item.QuarantinedQuantity,
        DiscardedQuantity = item.DiscardedQuantity,
        RejectedQuantity = item.RejectedQuantity,
        UnitValueSnapshot = item.UnitValueSnapshot,
        LineEstimatedAmount = item.LineEstimatedAmount,
        Condition = item.Condition,
        InspectionNotes = item.InspectionNotes,
        InspectedAtUtc = item.InspectedAtUtc,
        InspectedByUserId = item.InspectedByUserId
    };
}

public sealed class ReturnResponse
{
    public required Guid Id { get; init; }

    public required string ReturnNumber { get; init; }

    public required ReturnType Type { get; init; }

    public required ReturnStatus Status { get; init; }

    public Guid? CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? PurchaseOrderId { get; init; }

    public string? SourceReference { get; init; }

    public required ReturnReason Reason { get; init; }

    public string? ReasonDescription { get; init; }

    public string? Notes { get; init; }

    public required decimal EstimatedAmount { get; init; }

    public required ReturnFinancialTreatment FinancialTreatment { get; init; }

    public required ReturnFinancialStatus FinancialStatus { get; init; }

    public required decimal EstimatedFinancialAmount { get; init; }

    public Guid? FinancialTitleId { get; init; }

    public string? FinancialNotes { get; init; }

    public string? AuthorizationReference { get; init; }

    public string? DispatchReference { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? ReceivedAtUtc { get; init; }

    public DateTime? InspectedAtUtc { get; init; }

    public DateTime? AuthorizedAtUtc { get; init; }

    public DateTime? DispatchedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public string? CancellationReason { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public required int Version { get; init; }

    public required IReadOnlyList<ReturnItemResponse> Items { get; init; }

    public static ReturnResponse FromEntity(ReturnRequest entity) => new()
    {
        Id = entity.Id,
        ReturnNumber = entity.ReturnNumber,
        Type = entity.Type,
        Status = entity.Status,
        CustomerId = entity.CustomerId,
        CustomerName = entity.CustomerName,
        SupplierId = entity.SupplierId,
        SupplierName = entity.SupplierName,
        OrderId = entity.OrderId,
        PurchaseOrderId = entity.PurchaseOrderId,
        SourceReference = entity.SourceReference,
        Reason = entity.Reason,
        ReasonDescription = entity.ReasonDescription,
        Notes = entity.Notes,
        EstimatedAmount = entity.EstimatedAmount,
        FinancialTreatment = entity.FinancialTreatment,
        FinancialStatus = entity.FinancialStatus,
        EstimatedFinancialAmount = entity.EstimatedFinancialAmount,
        FinancialTitleId = entity.FinancialTitleId,
        FinancialNotes = entity.FinancialNotes,
        AuthorizationReference = entity.AuthorizationReference,
        DispatchReference = entity.DispatchReference,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        ReceivedAtUtc = entity.ReceivedAtUtc,
        InspectedAtUtc = entity.InspectedAtUtc,
        AuthorizedAtUtc = entity.AuthorizedAtUtc,
        DispatchedAtUtc = entity.DispatchedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        CancelledAtUtc = entity.CancelledAtUtc,
        CancellationReason = entity.CancellationReason,
        CreatedByUserId = entity.CreatedByUserId,
        Version = entity.Version,
        Items = entity.Items.Select(ReturnItemResponse.FromItem).ToList()
    };
}

public sealed class ReturnSummaryResponse
{
    public required Guid Id { get; init; }

    public required string ReturnNumber { get; init; }

    public required ReturnType Type { get; init; }

    public required ReturnStatus Status { get; init; }

    public Guid? CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public Guid? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? PurchaseOrderId { get; init; }

    public string? SourceReference { get; init; }

    public required ReturnReason Reason { get; init; }

    public required decimal EstimatedAmount { get; init; }

    public required int ItemCount { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public static ReturnSummaryResponse FromEntity(ReturnRequest entity) => new()
    {
        Id = entity.Id,
        ReturnNumber = entity.ReturnNumber,
        Type = entity.Type,
        Status = entity.Status,
        CustomerId = entity.CustomerId,
        CustomerName = entity.CustomerName,
        SupplierId = entity.SupplierId,
        SupplierName = entity.SupplierName,
        OrderId = entity.OrderId,
        PurchaseOrderId = entity.PurchaseOrderId,
        SourceReference = entity.SourceReference,
        Reason = entity.Reason,
        EstimatedAmount = entity.EstimatedAmount,
        ItemCount = entity.Items.Count,
        CreatedAtUtc = entity.CreatedAtUtc
    };
}

public sealed class ReturnableItemResponse
{
    public required Guid SourceItemId { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public required int SourceQuantity { get; init; }

    public required int PreviouslyReturnedQuantity { get; init; }

    public required int AvailableToReturn { get; init; }

    public required decimal UnitValueSnapshot { get; init; }
}

public sealed class ReturnStockMovementResponse
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string Sku { get; init; }

    public required string ProductName { get; init; }

    public Guid? ReturnItemId { get; init; }

    public required StockMovementType MovementType { get; init; }

    public required int Quantity { get; init; }

    public required int PreviousQuantity { get; init; }

    public required int NewQuantity { get; init; }

    public string? Reason { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
