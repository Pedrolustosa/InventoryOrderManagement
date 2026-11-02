using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// One receiving operation against a purchase order. Receipts are kept even when the order is later cancelled.
/// </summary>
public class PurchaseReceipt
{
    public const int ReceiptNumberMaxLength = 32;
    public const int NotesMaxLength = 500;

    private readonly List<PurchaseReceiptItem> _items = [];

    private PurchaseReceipt()
    {
    }

    public Guid Id { get; private set; }

    public string ReceiptNumber { get; private set; } = null!;

    public Guid PurchaseOrderId { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<PurchaseReceiptItem> Items => _items;

    public static PurchaseReceipt Create(
        string receiptNumber,
        Guid purchaseOrderId,
        string? notes,
        IReadOnlyList<PurchaseReceiptItem> items)
    {
        if (purchaseOrderId == Guid.Empty)
        {
            throw new DomainException("Purchase order is required.");
        }

        if (items is null || items.Count == 0)
        {
            throw new DomainException("A receipt must contain at least one item.");
        }

        var seen = new HashSet<Guid>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (!seen.Add(item.PurchaseOrderItemId))
            {
                throw new PurchaseOrderConflictException("The same purchase order item cannot be received more than once in the same receipt.");
            }
        }

        var receipt = new PurchaseReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = NormalizeNumber(receiptNumber),
            PurchaseOrderId = purchaseOrderId,
            ReceivedAtUtc = DateTime.UtcNow,
            Notes = NormalizeNotes(notes)
        };

        foreach (var item in items)
        {
            item.AssignReceipt(receipt.Id);
            receipt._items.Add(item);
        }

        return receipt;
    }

    private static string NormalizeNumber(string receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            throw new DomainException("Receipt number is required.");
        }

        var normalized = receiptNumber.Trim();

        if (normalized.Length > ReceiptNumberMaxLength)
        {
            throw new DomainException($"Receipt number cannot exceed {ReceiptNumberMaxLength} characters.");
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
}
