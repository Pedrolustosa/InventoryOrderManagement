using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Finance.Dtos;

public sealed class CashAccountResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required CashAccountType AccountType { get; init; }

    public required decimal OpeningBalance { get; init; }

    public required DateOnly OpeningBalanceDate { get; init; }

    public required bool IsActive { get; init; }

    public required decimal CurrentBalance { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public static CashAccountResponse From(CashAccount account, decimal currentBalance) => new()
    {
        Id = account.Id,
        Name = account.Name,
        AccountType = account.AccountType,
        OpeningBalance = account.OpeningBalance,
        OpeningBalanceDate = account.OpeningBalanceDate,
        IsActive = account.IsActive,
        CurrentBalance = currentBalance,
        CreatedAtUtc = account.CreatedAtUtc,
        UpdatedAtUtc = account.UpdatedAtUtc
    };
}

public sealed class CashAccountBalanceResponse
{
    public required Guid CashAccountId { get; init; }

    public required decimal OpeningBalance { get; init; }

    public required decimal TotalInflows { get; init; }

    public required decimal TotalOutflows { get; init; }

    public required decimal CurrentBalance { get; init; }
}

public sealed class CashMovementResponse
{
    public required Guid Id { get; init; }

    public required Guid CashAccountId { get; init; }

    public Guid? FinancialSettlementId { get; init; }

    public required CashMovementDirection Direction { get; init; }

    public required decimal Amount { get; init; }

    public required string Description { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public static CashMovementResponse From(CashMovement movement) => new()
    {
        Id = movement.Id,
        CashAccountId = movement.CashAccountId,
        FinancialSettlementId = movement.FinancialSettlementId,
        Direction = movement.Direction,
        Amount = movement.Amount,
        Description = movement.Description,
        OccurredAtUtc = movement.OccurredAtUtc
    };
}

public sealed class FinanceDashboardResponse
{
    public required decimal OpenPayables { get; init; }

    public required decimal OpenReceivables { get; init; }

    public required decimal OverduePayables { get; init; }

    public required decimal OverdueReceivables { get; init; }

    public required decimal PaidInPeriod { get; init; }

    public required decimal ReceivedInPeriod { get; init; }

    public required decimal RealizedInflows { get; init; }

    public required decimal RealizedOutflows { get; init; }

    public required decimal RealizedNet { get; init; }

    public required decimal ProjectedInflows { get; init; }

    public required decimal ProjectedOutflows { get; init; }

    public required IReadOnlyList<CashAccountResponse> CashAccounts { get; init; }
}

public enum CashFlowNature
{
    Realized,
    Projected
}

public enum CashFlowGrouping
{
    Day,
    Week,
    Month
}

public sealed class CashFlowBucketResponse
{
    public required DateOnly PeriodStart { get; init; }

    public required decimal Inflows { get; init; }

    public required decimal Outflows { get; init; }

    public required decimal Net { get; init; }
}

public sealed class CashFlowResponse
{
    public required CashFlowNature Nature { get; init; }

    public required CashFlowGrouping Grouping { get; init; }

    public required decimal Inflows { get; init; }

    public required decimal Outflows { get; init; }

    public required decimal Net { get; init; }

    public required IReadOnlyList<CashFlowBucketResponse> Buckets { get; init; }
}

public sealed class AgingReportItemResponse
{
    public required Guid TitleId { get; init; }

    public required string TitleNumber { get; init; }

    public required FinancialTitleType Type { get; init; }

    public required Guid InstallmentId { get; init; }

    public required int InstallmentNumber { get; init; }

    public required string CounterpartyName { get; init; }

    public required FinancialCounterpartyType CounterpartyType { get; init; }

    public string? DocumentNumber { get; init; }

    public required DateOnly DueDate { get; init; }

    public required int DaysOverdue { get; init; }

    public required decimal Amount { get; init; }

    public required decimal SettledPrincipalAmount { get; init; }

    public required decimal OutstandingBalance { get; init; }

    public required FinancialInstallmentStatus Status { get; init; }
}
