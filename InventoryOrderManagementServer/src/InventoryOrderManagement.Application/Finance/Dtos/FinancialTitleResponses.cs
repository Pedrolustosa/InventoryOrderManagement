using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Finance.Dtos;

public sealed class FinancialInstallmentResponse
{
    public required Guid Id { get; init; }

    public required int InstallmentNumber { get; init; }

    public required DateOnly DueDate { get; init; }

    public required decimal Amount { get; init; }

    public required decimal SettledPrincipalAmount { get; init; }

    public required decimal OutstandingBalance { get; init; }

    public required FinancialInstallmentStatus Status { get; init; }
}

public sealed class FinancialSettlementResponse
{
    public required Guid Id { get; init; }

    public required Guid FinancialTitleId { get; init; }

    public required Guid FinancialInstallmentId { get; init; }

    public Guid? CashAccountId { get; init; }

    public required FinancialPaymentMethod PaymentMethod { get; init; }

    public required decimal SettledPrincipalAmount { get; init; }

    public required decimal DiscountAmount { get; init; }

    public required decimal InterestAmount { get; init; }

    public required decimal FineAmount { get; init; }

    public required decimal CashAmount { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public string? Reference { get; init; }

    public string? Notes { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public static FinancialSettlementResponse From(FinancialSettlement settlement) => new()
    {
        Id = settlement.Id,
        FinancialTitleId = settlement.FinancialTitleId,
        FinancialInstallmentId = settlement.FinancialInstallmentId,
        CashAccountId = settlement.CashAccountId,
        PaymentMethod = settlement.PaymentMethod,
        SettledPrincipalAmount = settlement.SettledPrincipalAmount,
        DiscountAmount = settlement.DiscountAmount,
        InterestAmount = settlement.InterestAmount,
        FineAmount = settlement.FineAmount,
        CashAmount = settlement.CashAmount,
        OccurredAtUtc = settlement.OccurredAtUtc,
        Reference = settlement.Reference,
        Notes = settlement.Notes,
        CreatedAtUtc = settlement.CreatedAtUtc,
        CreatedByUserId = settlement.CreatedByUserId
    };
}

public sealed class FinancialTitleResponse
{
    public required Guid Id { get; init; }

    public required string TitleNumber { get; init; }

    public required FinancialTitleType Type { get; init; }

    public required string Description { get; init; }

    public required FinancialCounterpartyType CounterpartyType { get; init; }

    public Guid? CounterpartyId { get; init; }

    public required string CounterpartyName { get; init; }

    public required FinancialSourceType SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string? SourceReference { get; init; }

    public string? DocumentNumber { get; init; }

    public required DateOnly IssueDate { get; init; }

    public required decimal TotalAmount { get; init; }

    public required decimal SettledPrincipalAmount { get; init; }

    public required decimal OutstandingBalance { get; init; }

    public required FinancialTitleStatus Status { get; init; }

    public string? Notes { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public string? CancellationReason { get; init; }

    public required IReadOnlyList<FinancialInstallmentResponse> Installments { get; init; }

    public required IReadOnlyList<FinancialSettlementResponse> Settlements { get; init; }

    public static FinancialTitleResponse From(FinancialTitle title, DateOnly today)
    {
        var cancelled = title.CancelledAtUtc.HasValue;
        return new FinancialTitleResponse
        {
            Id = title.Id,
            TitleNumber = title.TitleNumber,
            Type = title.Type,
            Description = title.Description,
            CounterpartyType = title.CounterpartyType,
            CounterpartyId = title.CounterpartyId,
            CounterpartyName = title.CounterpartyName,
            SourceType = title.SourceType,
            SourceId = title.SourceId,
            SourceReference = title.SourceReference,
            DocumentNumber = title.DocumentNumber,
            IssueDate = title.IssueDate,
            TotalAmount = title.TotalAmount,
            SettledPrincipalAmount = title.SettledPrincipalAmount,
            OutstandingBalance = title.OutstandingBalance,
            Status = title.StatusOn(today),
            Notes = title.Notes,
            CreatedAtUtc = title.CreatedAtUtc,
            UpdatedAtUtc = title.UpdatedAtUtc,
            CreatedByUserId = title.CreatedByUserId,
            CancelledAtUtc = title.CancelledAtUtc,
            CancellationReason = title.CancellationReason,
            Installments = title.Installments
                .OrderBy(installment => installment.InstallmentNumber)
                .Select(installment => new FinancialInstallmentResponse
                {
                    Id = installment.Id,
                    InstallmentNumber = installment.InstallmentNumber,
                    DueDate = installment.DueDate,
                    Amount = installment.Amount,
                    SettledPrincipalAmount = installment.SettledPrincipalAmount,
                    OutstandingBalance = installment.OutstandingBalance,
                    Status = installment.StatusOn(today, cancelled)
                })
                .ToArray(),
            Settlements = title.Settlements
                .OrderBy(settlement => settlement.OccurredAtUtc)
                .Select(FinancialSettlementResponse.From)
                .ToArray()
        };
    }
}

public sealed class FinancialTitleSummaryResponse
{
    public required Guid Id { get; init; }

    public required string TitleNumber { get; init; }

    public required string Description { get; init; }

    public required FinancialCounterpartyType CounterpartyType { get; init; }

    public Guid? CounterpartyId { get; init; }

    public required string CounterpartyName { get; init; }

    public required FinancialSourceType SourceType { get; init; }

    public string? DocumentNumber { get; init; }

    public required DateOnly IssueDate { get; init; }

    public required decimal TotalAmount { get; init; }

    public required decimal OutstandingBalance { get; init; }

    public required FinancialTitleStatus Status { get; init; }

    public required int InstallmentCount { get; init; }

    public static FinancialTitleSummaryResponse From(FinancialTitle title, DateOnly today) => new()
    {
        Id = title.Id,
        TitleNumber = title.TitleNumber,
        Description = title.Description,
        CounterpartyType = title.CounterpartyType,
        CounterpartyId = title.CounterpartyId,
        CounterpartyName = title.CounterpartyName,
        SourceType = title.SourceType,
        DocumentNumber = title.DocumentNumber,
        IssueDate = title.IssueDate,
        TotalAmount = title.TotalAmount,
        OutstandingBalance = title.OutstandingBalance,
        Status = title.StatusOn(today),
        InstallmentCount = title.Installments.Count
    };
}
