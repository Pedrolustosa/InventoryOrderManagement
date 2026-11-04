using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

public class ReturnRequest
{
    public const int ReturnNumberMaxLength = 32;
    public const int ReasonDescriptionMaxLength = 300;
    public const int NotesMaxLength = 500;
    public const int CancellationReasonMaxLength = 300;
    public const int AuthorizationReferenceMaxLength = 80;
    public const int DispatchReferenceMaxLength = 80;
    public const int FinancialNotesMaxLength = 500;
    public const int SourceReferenceMaxLength = 64;

    private readonly List<ReturnItem> _items = [];

    private ReturnRequest()
    {
    }

    public Guid Id { get; private set; }

    public string ReturnNumber { get; private set; } = null!;

    public ReturnType Type { get; private set; }

    public ReturnStatus Status { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string? CustomerName { get; private set; }

    public Guid? SupplierId { get; private set; }

    public string? SupplierName { get; private set; }

    public Guid? OrderId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }

    public string? SourceReference { get; private set; }

    public ReturnReason Reason { get; private set; }

    public string? ReasonDescription { get; private set; }

    public string? Notes { get; private set; }

    public decimal EstimatedAmount { get; private set; }

    public ReturnFinancialTreatment FinancialTreatment { get; private set; }

    public ReturnFinancialStatus FinancialStatus { get; private set; }

    public decimal EstimatedFinancialAmount { get; private set; }

    public Guid? FinancialTitleId { get; private set; }

    public string? FinancialNotes { get; private set; }

    public string? AuthorizationReference { get; private set; }

    public string? DispatchReference { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? ReceivedAtUtc { get; private set; }

    public DateTime? InspectedAtUtc { get; private set; }

    public DateTime? AuthorizedAtUtc { get; private set; }

    public DateTime? DispatchedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<ReturnItem> Items => _items;

    public static ReturnRequest CreateCustomer(
        string returnNumber,
        Guid customerId,
        string customerName,
        Guid orderId,
        string sourceReference,
        ReturnReason reason,
        string? reasonDescription,
        string? notes,
        ReturnFinancialTreatment financialTreatment,
        string? financialNotes,
        Guid? createdByUserId,
        IReadOnlyList<ReturnItem> items)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer is required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Sales order is required.");
        }

        EnsureCustomerFinancialTreatment(financialTreatment);

        var request = CreateCore(
            returnNumber,
            ReturnType.CustomerReturn,
            reason,
            reasonDescription,
            notes,
            financialTreatment,
            financialNotes,
            createdByUserId,
            items);

        request.CustomerId = customerId;
        request.CustomerName = NormalizePartyName(customerName, "Customer name");
        request.OrderId = orderId;
        request.SourceReference = NormalizeSourceReference(sourceReference);
        return request;
    }

    public static ReturnRequest CreateSupplier(
        string returnNumber,
        Guid supplierId,
        string supplierName,
        Guid purchaseOrderId,
        string sourceReference,
        ReturnReason reason,
        string? reasonDescription,
        string? notes,
        ReturnFinancialTreatment financialTreatment,
        string? financialNotes,
        Guid? createdByUserId,
        IReadOnlyList<ReturnItem> items)
    {
        if (supplierId == Guid.Empty)
        {
            throw new DomainException("Supplier is required.");
        }

        if (purchaseOrderId == Guid.Empty)
        {
            throw new DomainException("Purchase order is required.");
        }

        EnsureSupplierFinancialTreatment(financialTreatment);

        var request = CreateCore(
            returnNumber,
            ReturnType.SupplierReturn,
            reason,
            reasonDescription,
            notes,
            financialTreatment,
            financialNotes,
            createdByUserId,
            items);

        request.SupplierId = supplierId;
        request.SupplierName = NormalizePartyName(supplierName, "Supplier name");
        request.PurchaseOrderId = purchaseOrderId;
        request.SourceReference = NormalizeSourceReference(sourceReference);
        return request;
    }

    public void UpdateDraft(
        ReturnReason reason,
        string? reasonDescription,
        string? notes,
        ReturnFinancialTreatment financialTreatment,
        string? financialNotes,
        IReadOnlyList<ReturnItem> items)
    {
        EnsureStatus(ReturnStatus.Draft);

        if (Type == ReturnType.CustomerReturn)
        {
            EnsureCustomerFinancialTreatment(financialTreatment);
        }
        else
        {
            EnsureSupplierFinancialTreatment(financialTreatment);
        }

        ApplyReason(reason, reasonDescription);
        Notes = NormalizeNotes(notes);
        ApplyFinancialIntent(financialTreatment, financialNotes);
        ReplaceItems(items);
        Touch();
    }

    public void Receive(IReadOnlyList<(Guid ReturnItemId, int ReceivedQuantity)> receipts, string? notes)
    {
        EnsureCustomer();
        EnsureStatus(ReturnStatus.Draft);

        if (receipts is null || receipts.Count == 0)
        {
            throw new DomainException("At least one received quantity is required.");
        }

        var seen = new HashSet<Guid>();
        foreach (var receipt in receipts)
        {
            if (!seen.Add(receipt.ReturnItemId))
            {
                throw new DomainException("The same return item cannot be received more than once.");
            }

            var item = _items.SingleOrDefault(x => x.Id == receipt.ReturnItemId)
                ?? throw new ReturnConflictException("The item does not belong to this return.");

            item.ApplyReceive(receipt.ReceivedQuantity);
        }

        foreach (var item in _items)
        {
            if (!seen.Contains(item.Id))
            {
                item.ApplyReceive(0);
            }
        }

        if (_items.Sum(item => item.ReceivedQuantity) <= 0)
        {
            throw new DomainException("At least one unit must be received.");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = NormalizeNotes(notes);
        }

        Status = ReturnStatus.Received;
        ReceivedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void StartInspection()
    {
        EnsureCustomer();
        EnsureStatus(ReturnStatus.Received);
        Status = ReturnStatus.UnderInspection;
        Touch();
    }

    public void CompleteInspection(
        IReadOnlyList<InspectionLine> lines,
        string? notes,
        Guid? inspectedByUserId)
    {
        EnsureCustomer();
        EnsureStatus(ReturnStatus.UnderInspection);

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException("Inspection results are required for every received item.");
        }

        var receivedItems = _items.Where(item => item.ReceivedQuantity > 0).ToList();
        if (lines.Count != receivedItems.Count)
        {
            throw new ReturnConflictException("Every received item must be inspected exactly once.");
        }

        var seen = new HashSet<Guid>();
        foreach (var line in lines)
        {
            if (!seen.Add(line.ReturnItemId))
            {
                throw new DomainException("The same return item cannot be inspected more than once.");
            }

            var item = receivedItems.SingleOrDefault(x => x.Id == line.ReturnItemId)
                ?? throw new ReturnConflictException("The inspected item was not received on this return.");

            item.ApplyInspection(
                line.Condition,
                line.RestockedQuantity,
                line.QuarantinedQuantity,
                line.DiscardedQuantity,
                line.RejectedQuantity,
                line.InspectionNotes,
                inspectedByUserId);
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = NormalizeNotes(notes);
        }

        InspectedAtUtc = DateTime.UtcNow;
        CompletedAtUtc = InspectedAtUtc;

        var accepted = _items.Sum(item => item.AcceptedQuantity);
        Status = accepted > 0 ? ReturnStatus.Completed : ReturnStatus.Rejected;
        Touch();
    }

    public void Authorize(string? notes, string? authorizationReference)
    {
        EnsureSupplier();
        EnsureStatus(ReturnStatus.Draft);

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = NormalizeNotes(notes);
        }

        AuthorizationReference = NormalizeOptional(
            authorizationReference,
            AuthorizationReferenceMaxLength,
            "Authorization reference");
        Status = ReturnStatus.Authorized;
        AuthorizedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Dispatch(string? notes, string? dispatchReference)
    {
        EnsureSupplier();
        EnsureStatus(ReturnStatus.Authorized);

        foreach (var item in _items)
        {
            item.ApplyDispatch();
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = NormalizeNotes(notes);
        }

        DispatchReference = NormalizeOptional(
            dispatchReference,
            DispatchReferenceMaxLength,
            "Dispatch reference");
        Status = ReturnStatus.Dispatched;
        DispatchedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Cancel(string reason)
    {
        if (Type == ReturnType.CustomerReturn)
        {
            if (Status is not (ReturnStatus.Draft or ReturnStatus.Received))
            {
                throw new InvalidReturnStatusException(Status);
            }
        }
        else
        {
            if (Status is not (ReturnStatus.Draft or ReturnStatus.Authorized))
            {
                throw new InvalidReturnStatusException(Status);
            }
        }

        CancellationReason = NormalizeCancellationReason(reason);
        Status = ReturnStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        Touch();
    }

    private static ReturnRequest CreateCore(
        string returnNumber,
        ReturnType type,
        ReturnReason reason,
        string? reasonDescription,
        string? notes,
        ReturnFinancialTreatment financialTreatment,
        string? financialNotes,
        Guid? createdByUserId,
        IReadOnlyList<ReturnItem> items)
    {
        var request = new ReturnRequest
        {
            Id = Guid.NewGuid(),
            ReturnNumber = NormalizeReturnNumber(returnNumber),
            Type = type,
            Status = ReturnStatus.Draft,
            Notes = NormalizeNotes(notes),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            Version = 0
        };

        request.ApplyReason(reason, reasonDescription);
        request.ApplyFinancialIntent(financialTreatment, financialNotes);
        request.ReplaceItems(items);
        return request;
    }

    private void ReplaceItems(IReadOnlyList<ReturnItem> items)
    {
        if (items is null || items.Count == 0)
        {
            throw new DomainException("A return must contain at least one item.");
        }

        var seenProducts = new HashSet<Guid>();
        var seenSourceItems = new HashSet<Guid>();

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (!seenProducts.Add(item.ProductId))
            {
                throw new DomainException("The same product cannot appear more than once on a return.");
            }

            if (Type == ReturnType.CustomerReturn)
            {
                if (item.OrderItemId is null || item.PurchaseOrderItemId is not null)
                {
                    throw new DomainException("Customer returns require sales order items.");
                }

                if (!seenSourceItems.Add(item.OrderItemId.Value))
                {
                    throw new DomainException("The same order item cannot appear more than once on a return.");
                }
            }
            else
            {
                if (item.PurchaseOrderItemId is null || item.OrderItemId is not null)
                {
                    throw new DomainException("Supplier returns require purchase order items.");
                }

                if (!seenSourceItems.Add(item.PurchaseOrderItemId.Value))
                {
                    throw new DomainException("The same purchase order item cannot appear more than once on a return.");
                }
            }
        }

        _items.Clear();
        foreach (var item in items)
        {
            item.AssignReturnRequest(Id);
            _items.Add(item);
        }

        EstimatedAmount = decimal.Round(_items.Sum(item => item.LineEstimatedAmount), 2, MidpointRounding.AwayFromZero);
        EstimatedFinancialAmount = EstimatedAmount;
    }

    private void ApplyReason(ReturnReason reason, string? reasonDescription)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new DomainException("Return reason is invalid.");
        }

        Reason = reason;

        if (reason == ReturnReason.Other)
        {
            if (string.IsNullOrWhiteSpace(reasonDescription))
            {
                throw new DomainException("Reason description is required when the reason is Other.");
            }
        }

        ReasonDescription = NormalizeOptional(reasonDescription, ReasonDescriptionMaxLength, "Reason description");
    }

    private void ApplyFinancialIntent(ReturnFinancialTreatment treatment, string? financialNotes)
    {
        if (!Enum.IsDefined(treatment))
        {
            throw new DomainException("Financial treatment is invalid.");
        }

        FinancialTreatment = treatment;
        FinancialStatus = treatment == ReturnFinancialTreatment.None
            ? ReturnFinancialStatus.None
            : ReturnFinancialStatus.Pending;
        FinancialNotes = NormalizeOptional(financialNotes, FinancialNotesMaxLength, "Financial notes");
        FinancialTitleId = null;
    }

    private void EnsureCustomer()
    {
        if (Type != ReturnType.CustomerReturn)
        {
            throw new DomainException("This operation applies only to customer returns.");
        }
    }

    private void EnsureSupplier()
    {
        if (Type != ReturnType.SupplierReturn)
        {
            throw new DomainException("This operation applies only to supplier returns.");
        }
    }

    private void EnsureStatus(ReturnStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidReturnStatusException(Status);
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }

    private static void EnsureCustomerFinancialTreatment(ReturnFinancialTreatment treatment)
    {
        if (treatment is ReturnFinancialTreatment.SupplierCredit or ReturnFinancialTreatment.SupplierRefund)
        {
            throw new DomainException("Customer returns cannot use a supplier financial treatment.");
        }
    }

    private static void EnsureSupplierFinancialTreatment(ReturnFinancialTreatment treatment)
    {
        if (treatment is ReturnFinancialTreatment.CustomerCredit or ReturnFinancialTreatment.CustomerRefund)
        {
            throw new DomainException("Supplier returns cannot use a customer financial treatment.");
        }
    }

    private static string NormalizeReturnNumber(string returnNumber)
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
        {
            throw new DomainException("Return number is required.");
        }

        var normalized = returnNumber.Trim();
        if (normalized.Length > ReturnNumberMaxLength)
        {
            throw new DomainException($"Return number cannot exceed {ReturnNumberMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizePartyName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException($"{fieldName} cannot exceed 200 characters.");
        }

        return normalized;
    }

    private static string NormalizeSourceReference(string sourceReference)
    {
        if (string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new DomainException("Source reference is required.");
        }

        var normalized = sourceReference.Trim();
        if (normalized.Length > SourceReferenceMaxLength)
        {
            throw new DomainException($"Source reference cannot exceed {SourceReferenceMaxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeNotes(string? notes)
        => NormalizeOptional(notes, NotesMaxLength, "Notes");

    private static string NormalizeCancellationReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancellation reason is required.");
        }

        var normalized = reason.Trim();
        if (normalized.Length > CancellationReasonMaxLength)
        {
            throw new DomainException($"Cancellation reason cannot exceed {CancellationReasonMaxLength} characters.");
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

public sealed record InspectionLine(
    Guid ReturnItemId,
    ReturnItemCondition Condition,
    int RestockedQuantity,
    int QuarantinedQuantity,
    int DiscardedQuantity,
    int RejectedQuantity,
    string? InspectionNotes);
