using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Finance.Dtos;

public sealed class FinancialInstallmentRequest
{
    public int InstallmentNumber { get; init; }

    public DateOnly DueDate { get; init; }

    public decimal Amount { get; init; }
}

public sealed class CreatePayableRequest
{
    public string Description { get; init; } = string.Empty;

    public Guid? SupplierId { get; init; }

    public string? CounterpartyName { get; init; }

    public FinancialSourceType SourceType { get; init; } = FinancialSourceType.Manual;

    public Guid? SourceId { get; init; }

    public string? DocumentNumber { get; init; }

    public DateOnly IssueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<FinancialInstallmentRequest>? Installments { get; init; }
}

public sealed class CreateReceivableRequest
{
    public string Description { get; init; } = string.Empty;

    public Guid? CustomerId { get; init; }

    public string? CounterpartyName { get; init; }

    public FinancialSourceType SourceType { get; init; } = FinancialSourceType.Manual;

    public Guid? SourceId { get; init; }

    public string? DocumentNumber { get; init; }

    public DateOnly IssueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<FinancialInstallmentRequest>? Installments { get; init; }
}

public sealed class UpdateFinancialTitleRequest
{
    public string Description { get; init; } = string.Empty;

    public Guid? CounterpartyId { get; init; }

    public string? CounterpartyName { get; init; }

    public FinancialSourceType SourceType { get; init; } = FinancialSourceType.Manual;

    public Guid? SourceId { get; init; }

    public string? DocumentNumber { get; init; }

    public DateOnly IssueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<FinancialInstallmentRequest>? Installments { get; init; }
}

public sealed class CancelFinancialTitleRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class SettleInstallmentRequest
{
    public Guid? CashAccountId { get; init; }

    public FinancialPaymentMethod PaymentMethod { get; init; }

    public decimal SettledPrincipalAmount { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal InterestAmount { get; init; }

    public decimal FineAmount { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public string? Reference { get; init; }

    public string? Notes { get; init; }

    public string? IdempotencyKey { get; init; }
}

public sealed class CreateCashAccountRequest
{
    public string Name { get; init; } = string.Empty;

    public CashAccountType AccountType { get; init; }

    public decimal OpeningBalance { get; init; }

    public DateOnly OpeningBalanceDate { get; init; }
}

public sealed class UpdateCashAccountRequest
{
    public string Name { get; init; } = string.Empty;

    public CashAccountType AccountType { get; init; }

    public decimal OpeningBalance { get; init; }

    public DateOnly OpeningBalanceDate { get; init; }
}
