using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Domain.Entities;

public class FinancialSettlement
{
    public const int ReferenceMaxLength = 80;
    public const int NotesMaxLength = 500;
    public const int IdempotencyKeyMaxLength = 80;

    private FinancialSettlement()
    {
    }

    public Guid Id { get; private set; }

    public Guid FinancialTitleId { get; private set; }

    public Guid FinancialInstallmentId { get; private set; }

    public Guid? CashAccountId { get; private set; }

    public FinancialPaymentMethod PaymentMethod { get; private set; }

    public decimal SettledPrincipalAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal InterestAmount { get; private set; }

    public decimal FineAmount { get; private set; }

    public decimal CashAmount { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? Reference { get; private set; }

    public string? Notes { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    internal static FinancialSettlement Create(
        Guid financialTitleId,
        Guid financialInstallmentId,
        Guid? cashAccountId,
        FinancialPaymentMethod paymentMethod,
        decimal settledPrincipalAmount,
        decimal discountAmount,
        decimal interestAmount,
        decimal fineAmount,
        DateTime occurredAtUtc,
        string? reference,
        string? notes,
        string? idempotencyKey,
        DateTime createdAtUtc,
        Guid? createdByUserId)
    {
        if (!Enum.IsDefined(paymentMethod))
        {
            throw new DomainException("Payment method is required.");
        }

        var principal = Money.Require(settledPrincipalAmount, "Settled principal amount");
        var discount = Money.Require(discountAmount, "Discount amount", allowZero: true);
        var interest = Money.Require(interestAmount, "Interest amount", allowZero: true);
        var fine = Money.Require(fineAmount, "Fine amount", allowZero: true);

        if (discount > principal)
        {
            throw new DomainException("Discount cannot exceed the settled principal amount.");
        }

        var cashAmount = Money.Round(principal - discount + interest + fine);
        if (cashAmount < 0)
        {
            throw new DomainException("The cash amount cannot be negative.");
        }

        if (cashAmount > 0 && cashAccountId is null)
        {
            throw new DomainException("An active cash account is required when cash is moved.");
        }

        if (cashAmount == 0)
        {
            cashAccountId = null;
        }

        if (occurredAtUtc == default)
        {
            throw new DomainException("Settlement date is required.");
        }

        return new FinancialSettlement
        {
            Id = Guid.NewGuid(),
            FinancialTitleId = financialTitleId,
            FinancialInstallmentId = financialInstallmentId,
            CashAccountId = cashAccountId,
            PaymentMethod = paymentMethod,
            SettledPrincipalAmount = principal,
            DiscountAmount = discount,
            InterestAmount = interest,
            FineAmount = fine,
            CashAmount = cashAmount,
            OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc),
            Reference = NormalizeOptional(reference, ReferenceMaxLength, "Reference"),
            Notes = NormalizeOptional(notes, NotesMaxLength, "Notes"),
            IdempotencyKey = NormalizeOptional(idempotencyKey, IdempotencyKeyMaxLength, "Idempotency key"),
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId
        };
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
