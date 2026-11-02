using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Finance;

public sealed class FinancialTitleListFilter
{
    public FinancialTitleType Type { get; init; }

    public Guid? CounterpartyId { get; init; }

    public string? DocumentNumber { get; init; }

    public DateOnly? IssueFrom { get; init; }

    public DateOnly? IssueTo { get; init; }

    public DateOnly? DueFrom { get; init; }

    public DateOnly? DueTo { get; init; }
}

public sealed class FinancialInstallmentRow
{
    public required Guid TitleId { get; init; }

    public required string TitleNumber { get; init; }

    public required FinancialTitleType Type { get; init; }

    public required FinancialCounterpartyType CounterpartyType { get; init; }

    public Guid? CounterpartyId { get; init; }

    public required string CounterpartyName { get; init; }

    public string? DocumentNumber { get; init; }

    public required DateOnly IssueDate { get; init; }

    public required bool TitleCancelled { get; init; }

    public required Guid InstallmentId { get; init; }

    public required int InstallmentNumber { get; init; }

    public required DateOnly DueDate { get; init; }

    public required decimal Amount { get; init; }

    public required decimal SettledPrincipalAmount { get; init; }

    public required decimal OutstandingBalance { get; init; }
}

public sealed class CashMovementListFilter
{
    public Guid? CashAccountId { get; init; }

    public DateTime? StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }
}

public sealed class FinancialSettlementListFilter
{
    public FinancialTitleType? Type { get; init; }

    public Guid? CashAccountId { get; init; }

    public DateTime? StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }
}
