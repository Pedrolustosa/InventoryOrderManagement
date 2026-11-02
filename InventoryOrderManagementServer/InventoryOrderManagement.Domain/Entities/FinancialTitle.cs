using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Domain.Entities;

public sealed record FinancialSettlementResult(FinancialSettlement Settlement, CashMovement? Movement);

public class FinancialTitle
{
    public const int TitleNumberMaxLength = 32;
    public const int DescriptionMaxLength = 200;
    public const int CounterpartyNameMaxLength = 150;
    public const int SourceReferenceMaxLength = 64;
    public const int DocumentNumberMaxLength = 50;
    public const int NotesMaxLength = 500;
    public const int CancellationReasonMaxLength = 300;

    private readonly List<FinancialInstallment> _installments = [];
    private readonly List<FinancialSettlement> _settlements = [];

    private FinancialTitle()
    {
    }

    public Guid Id { get; private set; }

    public string TitleNumber { get; private set; } = null!;

    public FinancialTitleType Type { get; private set; }

    public string Description { get; private set; } = null!;

    public FinancialCounterpartyType CounterpartyType { get; private set; }

    public Guid? CounterpartyId { get; private set; }

    public string CounterpartyName { get; private set; } = null!;

    public FinancialSourceType SourceType { get; private set; }

    public Guid? SourceId { get; private set; }

    public string? SourceReference { get; private set; }

    public string? DocumentNumber { get; private set; }

    public DateOnly IssueDate { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<FinancialInstallment> Installments => _installments;

    public IReadOnlyCollection<FinancialSettlement> Settlements => _settlements;

    public decimal SettledPrincipalAmount => _installments.Sum(installment => installment.SettledPrincipalAmount);

    public decimal OutstandingBalance => _installments.Sum(installment => installment.OutstandingBalance);

    public bool HasSettlements => _installments.Any(installment => installment.SettledPrincipalAmount > 0);

    public FinancialTitleStatus StatusOn(DateOnly today) =>
        FinancialStatus.Title(
            CancelledAtUtc.HasValue,
            SettledPrincipalAmount,
            OutstandingBalance,
            _installments.Any(installment => installment.StatusOn(today, CancelledAtUtc.HasValue) == FinancialInstallmentStatus.Overdue));

    public static FinancialTitle Create(
        string titleNumber,
        FinancialTitleType type,
        string description,
        FinancialCounterpartyType counterpartyType,
        Guid? counterpartyId,
        string counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        string? sourceReference,
        string? documentNumber,
        DateOnly issueDate,
        decimal totalAmount,
        string? notes,
        Guid? createdByUserId,
        IReadOnlyList<(int InstallmentNumber, DateOnly DueDate, decimal Amount)> installments,
        DateTime createdAtUtc)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Financial title type is required.");
        }

        var title = new FinancialTitle
        {
            Id = Guid.NewGuid(),
            TitleNumber = NormalizeRequired(titleNumber, TitleNumberMaxLength, "Title number"),
            Type = type,
            Description = NormalizeRequired(description, DescriptionMaxLength, "Description"),
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId,
            Version = 0
        };

        title.ApplyIdentity(
            counterpartyType,
            counterpartyId,
            counterpartyName,
            sourceType,
            sourceId,
            sourceReference,
            documentNumber,
            issueDate,
            totalAmount,
            notes,
            installments,
            createdAtUtc);

        return title;
    }

    public IReadOnlyCollection<FinancialInstallment> Update(
        string description,
        FinancialCounterpartyType counterpartyType,
        Guid? counterpartyId,
        string counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        string? sourceReference,
        string? documentNumber,
        DateOnly issueDate,
        decimal totalAmount,
        string? notes,
        IReadOnlyList<(int InstallmentNumber, DateOnly DueDate, decimal Amount)> installments,
        DateTime updatedAtUtc)
    {
        EnsureEditable();
        var removed = _installments.ToArray();
        _installments.Clear();
        ApplyIdentity(
            counterpartyType,
            counterpartyId,
            counterpartyName,
            sourceType,
            sourceId,
            sourceReference,
            documentNumber,
            issueDate,
            totalAmount,
            notes,
            installments,
            updatedAtUtc);
        Description = NormalizeRequired(description, DescriptionMaxLength, "Description");
        Touch(updatedAtUtc);
        return removed;
    }

    public void Cancel(string reason, DateTime cancelledAtUtc)
    {
        if (CancelledAtUtc.HasValue)
        {
            throw new FinancialConflictException("The financial title is already cancelled.");
        }

        if (_installments.All(installment => installment.OutstandingBalance == 0))
        {
            throw new FinancialConflictException("A fully settled financial title cannot be cancelled.");
        }

        CancellationReason = NormalizeRequired(reason, CancellationReasonMaxLength, "Cancellation reason");
        CancelledAtUtc = cancelledAtUtc;
        Touch(cancelledAtUtc);
    }

    public FinancialSettlementResult SettleInstallment(
        Guid installmentId,
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
        if (CancelledAtUtc.HasValue)
        {
            throw new FinancialConflictException("A cancelled financial title cannot be settled.");
        }

        var installment = _installments.SingleOrDefault(item => item.Id == installmentId)
            ?? throw new FinancialConflictException("The installment does not belong to this financial title.");

        if (installment.OutstandingBalance == 0)
        {
            throw new FinancialConflictException("The installment is already settled.");
        }

        var settlement = FinancialSettlement.Create(
            Id,
            installment.Id,
            cashAccountId,
            paymentMethod,
            settledPrincipalAmount,
            discountAmount,
            interestAmount,
            fineAmount,
            occurredAtUtc,
            reference,
            notes,
            idempotencyKey,
            createdAtUtc,
            createdByUserId);

        installment.ApplyPrincipal(settlement.SettledPrincipalAmount);
        _settlements.Add(settlement);

        CashMovement? movement = null;
        if (settlement.CashAmount > 0)
        {
            movement = CashMovement.Create(
                settlement.CashAccountId!.Value,
                settlement.Id,
                Type == FinancialTitleType.Payable ? CashMovementDirection.Outflow : CashMovementDirection.Inflow,
                settlement.CashAmount,
                $"{TitleNumber} installment {installment.InstallmentNumber}",
                settlement.OccurredAtUtc,
                createdAtUtc);
            movement.Attach(settlement);
        }

        Touch(createdAtUtc);
        return new FinancialSettlementResult(settlement, movement);
    }

    private void ApplyIdentity(
        FinancialCounterpartyType counterpartyType,
        Guid? counterpartyId,
        string counterpartyName,
        FinancialSourceType sourceType,
        Guid? sourceId,
        string? sourceReference,
        string? documentNumber,
        DateOnly issueDate,
        decimal totalAmount,
        string? notes,
        IReadOnlyList<(int InstallmentNumber, DateOnly DueDate, decimal Amount)> installments,
        DateTime timestampUtc)
    {
        if (!Enum.IsDefined(counterpartyType))
        {
            throw new DomainException("Counterparty type is required.");
        }

        if (!Enum.IsDefined(sourceType))
        {
            throw new DomainException("Source type is required.");
        }

        if (counterpartyType == FinancialCounterpartyType.Other)
        {
            if (counterpartyId.HasValue)
            {
                throw new DomainException("An other counterparty cannot reference a supplier or customer.");
            }
        }
        else if (counterpartyId is null || counterpartyId == Guid.Empty)
        {
            throw new DomainException("Counterparty is required.");
        }

        if (sourceType == FinancialSourceType.Manual)
        {
            if (sourceId.HasValue)
            {
                throw new DomainException("A manual title cannot reference a source document.");
            }
        }
        else if (sourceId is null || sourceId == Guid.Empty)
        {
            throw new DomainException("Source document is required.");
        }

        if (sourceType == FinancialSourceType.PurchaseOrder && Type != FinancialTitleType.Payable)
        {
            throw new DomainException("A purchase order can only originate a payable.");
        }

        if (sourceType == FinancialSourceType.SalesOrder && Type != FinancialTitleType.Receivable)
        {
            throw new DomainException("A sales order can only originate a receivable.");
        }

        if (issueDate == default)
        {
            throw new DomainException("Issue date is required.");
        }

        var normalizedTotal = Money.Require(totalAmount, "Total amount");
        ReplaceInstallments(installments, normalizedTotal, timestampUtc);

        CounterpartyType = counterpartyType;
        CounterpartyId = counterpartyType == FinancialCounterpartyType.Other ? null : counterpartyId;
        CounterpartyName = NormalizeRequired(counterpartyName, CounterpartyNameMaxLength, "Counterparty name");
        SourceType = sourceType;
        SourceId = sourceType == FinancialSourceType.Manual ? null : sourceId;
        SourceReference = NormalizeOptional(sourceReference, SourceReferenceMaxLength, "Source reference");
        DocumentNumber = NormalizeOptional(documentNumber, DocumentNumberMaxLength, "Document number");
        IssueDate = issueDate;
        TotalAmount = normalizedTotal;
        Notes = NormalizeOptional(notes, NotesMaxLength, "Notes");
    }

    private void ReplaceInstallments(
        IReadOnlyList<(int InstallmentNumber, DateOnly DueDate, decimal Amount)> installments,
        decimal totalAmount,
        DateTime createdAtUtc)
    {
        if (installments is null || installments.Count == 0)
        {
            throw new DomainException("A financial title must contain at least one installment.");
        }

        var numbers = new HashSet<int>();
        decimal sum = 0;
        foreach (var installment in installments)
        {
            if (!numbers.Add(installment.InstallmentNumber))
            {
                throw new DomainException("Installment numbers must be unique.");
            }

            var created = FinancialInstallment.Create(Id, installment.InstallmentNumber, installment.DueDate, installment.Amount, createdAtUtc);
            sum = Money.Round(sum + created.Amount);
            _installments.Add(created);
        }

        if (sum != totalAmount)
        {
            throw new DomainException("The installments must add up to the total amount.");
        }
    }

    private void EnsureEditable()
    {
        if (CancelledAtUtc.HasValue)
        {
            throw new FinancialConflictException("A cancelled financial title cannot be edited.");
        }

        if (HasSettlements || OutstandingBalance == 0)
        {
            throw new FinancialConflictException("A financial title with settlements cannot be edited.");
        }
    }

    private void Touch(DateTime timestampUtc)
    {
        UpdatedAtUtc = timestampUtc;
        Version++;
    }

    private static string NormalizeRequired(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(value, maxLength, name);
    }
}
