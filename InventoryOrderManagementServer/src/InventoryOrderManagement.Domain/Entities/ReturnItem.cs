using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

public class ReturnItem
{
    public const int InspectionNotesMaxLength = 500;

    private ReturnItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReturnRequestId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public Guid? OrderItemId { get; private set; }

    public Guid? PurchaseOrderItemId { get; private set; }

    public int SourceQuantity { get; private set; }

    public int PreviouslyReturnedQuantity { get; private set; }

    public int RequestedQuantity { get; private set; }

    public int ReceivedQuantity { get; private set; }

    public int DispatchedQuantity { get; private set; }

    public int RestockedQuantity { get; private set; }

    public int QuarantinedQuantity { get; private set; }

    public int DiscardedQuantity { get; private set; }

    public int RejectedQuantity { get; private set; }

    public decimal UnitValueSnapshot { get; private set; }

    public decimal LineEstimatedAmount { get; private set; }

    public ReturnItemCondition? Condition { get; private set; }

    public string? InspectionNotes { get; private set; }

    public DateTime? InspectedAtUtc { get; private set; }

    public Guid? InspectedByUserId { get; private set; }

    public int AcceptedQuantity => RestockedQuantity + QuarantinedQuantity + DiscardedQuantity;

    public static ReturnItem CreateForCustomer(
        Guid productId,
        string productSku,
        string productName,
        Guid orderItemId,
        int sourceQuantity,
        int previouslyReturnedQuantity,
        int requestedQuantity,
        decimal unitPrice)
    {
        if (orderItemId == Guid.Empty)
        {
            throw new DomainException("Order item is required.");
        }

        var item = CreateCore(
            productId,
            productSku,
            productName,
            sourceQuantity,
            previouslyReturnedQuantity,
            requestedQuantity,
            unitPrice);

        item.OrderItemId = orderItemId;
        return item;
    }

    public static ReturnItem CreateForSupplier(
        Guid productId,
        string productSku,
        string productName,
        Guid purchaseOrderItemId,
        int sourceQuantity,
        int previouslyReturnedQuantity,
        int requestedQuantity,
        decimal unitCost)
    {
        if (purchaseOrderItemId == Guid.Empty)
        {
            throw new DomainException("Purchase order item is required.");
        }

        var item = CreateCore(
            productId,
            productSku,
            productName,
            sourceQuantity,
            previouslyReturnedQuantity,
            requestedQuantity,
            unitCost);

        item.PurchaseOrderItemId = purchaseOrderItemId;
        return item;
    }

    internal void AssignReturnRequest(Guid returnRequestId)
    {
        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        ReturnRequestId = returnRequestId;
    }

    internal void ApplyReceive(int receivedQuantity)
    {
        if (receivedQuantity < 0)
        {
            throw new DomainException("Received quantity cannot be negative.");
        }

        if (receivedQuantity > RequestedQuantity)
        {
            throw new ReturnConflictException("Received quantity cannot exceed the requested quantity.");
        }

        ReceivedQuantity = receivedQuantity;
    }

    internal void ApplyInspection(
        ReturnItemCondition condition,
        int restockedQuantity,
        int quarantinedQuantity,
        int discardedQuantity,
        int rejectedQuantity,
        string? inspectionNotes,
        Guid? inspectedByUserId)
    {
        if (!Enum.IsDefined(condition))
        {
            throw new DomainException("Inspection condition is invalid.");
        }

        EnsureNonNegative(restockedQuantity, "Restocked");
        EnsureNonNegative(quarantinedQuantity, "Quarantined");
        EnsureNonNegative(discardedQuantity, "Discarded");
        EnsureNonNegative(rejectedQuantity, "Rejected");

        var total = restockedQuantity + quarantinedQuantity + discardedQuantity + rejectedQuantity;
        if (total != ReceivedQuantity)
        {
            throw new ReturnConflictException(
                "Inspected quantities must equal the received quantity for each item.");
        }

        var notes = NormalizeInspectionNotes(inspectionNotes, condition == ReturnItemCondition.Other);

        Condition = condition;
        RestockedQuantity = restockedQuantity;
        QuarantinedQuantity = quarantinedQuantity;
        DiscardedQuantity = discardedQuantity;
        RejectedQuantity = rejectedQuantity;
        InspectionNotes = notes;
        InspectedAtUtc = DateTime.UtcNow;
        InspectedByUserId = inspectedByUserId;
    }

    internal void ApplyDispatch()
    {
        DispatchedQuantity = RequestedQuantity;
    }

    internal void ReplaceRequestedQuantity(int requestedQuantity, int sourceQuantity, int previouslyReturnedQuantity)
    {
        ValidateRequestedQuantity(requestedQuantity, sourceQuantity, previouslyReturnedQuantity);
        SourceQuantity = sourceQuantity;
        PreviouslyReturnedQuantity = previouslyReturnedQuantity;
        RequestedQuantity = requestedQuantity;
        LineEstimatedAmount = decimal.Round(requestedQuantity * UnitValueSnapshot, 2, MidpointRounding.AwayFromZero);
    }

    private static ReturnItem CreateCore(
        Guid productId,
        string productSku,
        string productName,
        int sourceQuantity,
        int previouslyReturnedQuantity,
        int requestedQuantity,
        decimal unitValue)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (sourceQuantity <= 0)
        {
            throw new DomainException("Source quantity must be a positive integer.");
        }

        if (previouslyReturnedQuantity < 0)
        {
            throw new DomainException("Previously returned quantity cannot be negative.");
        }

        ValidateRequestedQuantity(requestedQuantity, sourceQuantity, previouslyReturnedQuantity);

        if (unitValue < 0)
        {
            throw new DomainException("Unit value cannot be negative.");
        }

        var price = decimal.Round(unitValue, 2, MidpointRounding.AwayFromZero);

        return new ReturnItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductSku = NormalizeRequired(productSku, Product.SkuMaxLength, "Product SKU"),
            ProductName = NormalizeRequired(productName, Product.NameMaxLength, "Product name"),
            SourceQuantity = sourceQuantity,
            PreviouslyReturnedQuantity = previouslyReturnedQuantity,
            RequestedQuantity = requestedQuantity,
            UnitValueSnapshot = price,
            LineEstimatedAmount = decimal.Round(requestedQuantity * price, 2, MidpointRounding.AwayFromZero)
        };
    }

    private static void ValidateRequestedQuantity(int requestedQuantity, int sourceQuantity, int previouslyReturnedQuantity)
    {
        if (requestedQuantity <= 0)
        {
            throw new DomainException("Requested quantity must be a positive integer.");
        }

        var available = sourceQuantity - previouslyReturnedQuantity;
        if (available < 0)
        {
            available = 0;
        }

        if (requestedQuantity > available)
        {
            throw new ReturnConflictException(
                "Requested quantity exceeds the quantity available to return.");
        }
    }

    private static void EnsureNonNegative(int quantity, string label)
    {
        if (quantity < 0)
        {
            throw new DomainException($"{label} quantity cannot be negative.");
        }
    }

    private static string? NormalizeInspectionNotes(string? notes, bool required)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            if (required)
            {
                throw new DomainException("Inspection notes are required when the condition is Other.");
            }

            return null;
        }

        var normalized = notes.Trim();
        if (normalized.Length > InspectionNotesMaxLength)
        {
            throw new DomainException($"Inspection notes cannot exceed {InspectionNotesMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
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
}
