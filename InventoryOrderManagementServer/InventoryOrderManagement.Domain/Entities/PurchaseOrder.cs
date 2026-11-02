using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Aggregate root for a supplier purchase order. Status changes only through the transition methods.
/// </summary>
public class PurchaseOrder
{
    public const int PurchaseOrderNumberMaxLength = 32;
    public const int NotesMaxLength = 500;
    public const int CancellationReasonMaxLength = 300;

    private readonly List<PurchaseOrderItem> _items = [];

    private PurchaseOrder()
    {
    }

    public Guid Id { get; private set; }

    public string PurchaseOrderNumber { get; private set; } = null!;

    public Guid SupplierId { get; private set; }

    public string SupplierName { get; private set; } = null!;

    public PurchaseOrderStatus Status { get; private set; }

    public IReadOnlyCollection<PurchaseOrderItem> Items => _items;

    public decimal Subtotal { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public DateTime? ReceivedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public int Version { get; private set; }

    public static PurchaseOrder Create(
        string purchaseOrderNumber,
        Guid supplierId,
        string supplierName,
        string? notes,
        IReadOnlyList<PurchaseOrderItem> items)
    {
        if (supplierId == Guid.Empty)
        {
            throw new DomainException("Supplier is required.");
        }

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = NormalizeNumber(purchaseOrderNumber),
            SupplierId = supplierId,
            SupplierName = NormalizeSupplierName(supplierName),
            Status = PurchaseOrderStatus.Draft,
            Notes = NormalizeNotes(notes),
            CreatedAtUtc = DateTime.UtcNow,
            Version = 0
        };

        order.ReplaceItems(items);
        return order;
    }

    public void ReplaceDraft(string? notes, IReadOnlyList<PurchaseOrderItem> items)
    {
        EnsureStatus(PurchaseOrderStatus.Draft);
        Notes = NormalizeNotes(notes);
        ReplaceItems(items);
        Touch();
    }

    public void Send()
    {
        EnsureStatus(PurchaseOrderStatus.Draft);

        if (_items.Count == 0)
        {
            throw new DomainException("A purchase order must contain at least one item.");
        }

        Status = PurchaseOrderStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Receive(IReadOnlyList<(Guid PurchaseOrderItemId, int Quantity)> lines)
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new InvalidPurchaseOrderStatusException(Status);
        }

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException("A receipt must contain at least one item.");
        }

        var seen = new HashSet<Guid>();
        foreach (var line in lines)
        {
            if (!seen.Add(line.PurchaseOrderItemId))
            {
                throw new PurchaseOrderConflictException("The same purchase order item cannot be received more than once in the same receipt.");
            }

            var item = _items.SingleOrDefault(candidate => candidate.Id == line.PurchaseOrderItemId)
                ?? throw new PurchaseOrderConflictException("The item does not belong to this purchase order.");

            item.Receive(line.Quantity);
        }

        if (_items.All(item => item.PendingQuantity == 0))
        {
            Status = PurchaseOrderStatus.Received;
            ReceivedAtUtc = DateTime.UtcNow;
        }
        else
        {
            Status = PurchaseOrderStatus.PartiallyReceived;
        }

        Touch();
    }

    public void Cancel(string reason)
    {
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new InvalidPurchaseOrderStatusException(Status);
        }

        CancellationReason = NormalizeReason(reason);
        Status = PurchaseOrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        Touch();
    }

    private void ReplaceItems(IReadOnlyList<PurchaseOrderItem> items)
    {
        if (items is null || items.Count == 0)
        {
            throw new DomainException("A purchase order must contain at least one item.");
        }

        var seen = new HashSet<Guid>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (!seen.Add(item.ProductId))
            {
                throw new PurchaseOrderConflictException("The same product cannot be added more than once to a purchase order.");
            }
        }

        _items.Clear();

        foreach (var item in items)
        {
            item.AssignPurchaseOrder(Id);
            _items.Add(item);
        }

        Subtotal = _items.Sum(item => item.LineTotal);
    }

    private void EnsureStatus(PurchaseOrderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidPurchaseOrderStatusException(Status);
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }

    private static string NormalizeNumber(string purchaseOrderNumber)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
        {
            throw new DomainException("Purchase order number is required.");
        }

        var normalized = purchaseOrderNumber.Trim();

        if (normalized.Length > PurchaseOrderNumberMaxLength)
        {
            throw new DomainException($"Purchase order number cannot exceed {PurchaseOrderNumberMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeSupplierName(string supplierName)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new DomainException("Supplier name is required.");
        }

        var normalized = supplierName.Trim();

        if (normalized.Length > Supplier.NameMaxLength)
        {
            throw new DomainException($"Supplier name cannot exceed {Supplier.NameMaxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var normalized = notes.Trim();

        if (normalized.Length > NotesMaxLength)
        {
            throw new DomainException($"Notes cannot exceed {NotesMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeReason(string reason)
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
}
