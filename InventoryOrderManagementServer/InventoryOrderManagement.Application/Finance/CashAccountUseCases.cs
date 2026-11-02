using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Application.Finance;

public sealed class CreateCashAccountUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _audit;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public CreateCashAccountUseCase(
        ICashAccountRepository accounts,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter audit,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<CashAccountResponse> ExecuteAsync(CreateCashAccountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        CashAccount? created = null;
        await _unitOfWork.ExecuteAsync(ct =>
        {
            created = CashAccount.Create(
                request.Name,
                request.AccountType,
                request.OpeningBalance,
                request.OpeningBalanceDate,
                BusinessClock.UtcNow(_time));
            return _accounts.AddAsync(created, ct);
        }, cancellationToken);

        var account = created ?? throw new InvalidOperationException("Cash account was not created.");
        await FinanceAudit.WriteAsync(_audit, _currentUser, SecurityActions.CashAccountCreated, "CashAccount", account.Id, account.Name, cancellationToken);
        return CashAccountResponse.From(account, account.OpeningBalance);
    }
}

public sealed class UpdateCashAccountUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICashMovementRepository _movements;
    private readonly ISecurityAuditWriter _audit;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public UpdateCashAccountUseCase(
        ICashAccountRepository accounts,
        IUnitOfWork unitOfWork,
        ICashMovementRepository movements,
        ISecurityAuditWriter audit,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _movements = movements;
        _audit = audit;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<CashAccountResponse> ExecuteAsync(Guid id, UpdateCashAccountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        CashAccount? updated = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var account = await _accounts.GetTrackedByIdAsync(id, ct) ?? throw new CashAccountNotFoundException(id);
            account.Update(
                request.Name,
                request.AccountType,
                request.OpeningBalance,
                request.OpeningBalanceDate,
                await _accounts.HasMovementsAsync(id, ct),
                BusinessClock.UtcNow(_time));
            updated = account;
        }, cancellationToken);

        var stored = updated ?? throw new InvalidOperationException("Cash account was not updated.");
        var balance = await CurrentBalanceAsync(stored, cancellationToken);
        await FinanceAudit.WriteAsync(_audit, _currentUser, SecurityActions.CashAccountUpdated, "CashAccount", stored.Id, stored.Name, cancellationToken);
        return CashAccountResponse.From(stored, balance);
    }

    private async Task<decimal> CurrentBalanceAsync(CashAccount account, CancellationToken cancellationToken)
    {
        var movements = await _movements.ListAsync(new CashMovementListFilter { CashAccountId = account.Id }, cancellationToken);
        return CashBalances.Current(account.OpeningBalance, movements);
    }
}

public sealed class DeactivateCashAccountUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _audit;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public DeactivateCashAccountUseCase(
        ICashAccountRepository accounts,
        ICashMovementRepository movements,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter audit,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _accounts = accounts;
        _movements = movements;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<CashAccountResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CashAccount? updated = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var account = await _accounts.GetTrackedByIdAsync(id, ct) ?? throw new CashAccountNotFoundException(id);
            account.Deactivate(BusinessClock.UtcNow(_time));
            updated = account;
        }, cancellationToken);

        var stored = updated ?? throw new InvalidOperationException("Cash account was not deactivated.");
        var movements = await _movements.ListAsync(new CashMovementListFilter { CashAccountId = stored.Id }, cancellationToken);
        await FinanceAudit.WriteAsync(_audit, _currentUser, SecurityActions.CashAccountDeactivated, "CashAccount", stored.Id, stored.Name, cancellationToken);
        return CashAccountResponse.From(stored, CashBalances.Current(stored.OpeningBalance, movements));
    }
}

public sealed class GetAllCashAccountsUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;

    public GetAllCashAccountsUseCase(ICashAccountRepository accounts, ICashMovementRepository movements)
    {
        _accounts = accounts;
        _movements = movements;
    }

    public async Task<IReadOnlyList<CashAccountResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.ListAsync(cancellationToken);
        var movements = await _movements.ListAsync(new CashMovementListFilter(), cancellationToken);
        return accounts
            .Select(account => CashAccountResponse.From(
                account,
                CashBalances.Current(account.OpeningBalance, movements.Where(movement => movement.CashAccountId == account.Id))))
            .ToArray();
    }
}

public sealed class GetCashAccountByIdUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;

    public GetCashAccountByIdUseCase(ICashAccountRepository accounts, ICashMovementRepository movements)
    {
        _accounts = accounts;
        _movements = movements;
    }

    public async Task<CashAccountResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken) ?? throw new CashAccountNotFoundException(id);
        var movements = await _movements.ListAsync(new CashMovementListFilter { CashAccountId = id }, cancellationToken);
        return CashAccountResponse.From(account, CashBalances.Current(account.OpeningBalance, movements));
    }
}

public sealed class GetCashAccountBalanceUseCase
{
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;

    public GetCashAccountBalanceUseCase(ICashAccountRepository accounts, ICashMovementRepository movements)
    {
        _accounts = accounts;
        _movements = movements;
    }

    public async Task<CashAccountBalanceResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken) ?? throw new CashAccountNotFoundException(id);
        var movements = await _movements.ListAsync(new CashMovementListFilter { CashAccountId = id }, cancellationToken);
        var (inflows, outflows, current) = CashBalances.Summarize(account.OpeningBalance, movements);
        return new CashAccountBalanceResponse
        {
            CashAccountId = account.Id,
            OpeningBalance = account.OpeningBalance,
            TotalInflows = inflows,
            TotalOutflows = outflows,
            CurrentBalance = current
        };
    }
}

public sealed class GetFinancialSettlementsUseCase
{
    private readonly IFinancialSettlementRepository _settlements;

    public GetFinancialSettlementsUseCase(IFinancialSettlementRepository settlements)
    {
        _settlements = settlements;
    }

    public async Task<PagedResult<FinancialSettlementResponse>> ExecuteAsync(
        FinancialTitleType? type,
        Guid? cashAccountId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period: null, DateTime.UtcNow);
        var settlements = await _settlements.ListAsync(
            new FinancialSettlementListFilter
            {
                Type = type,
                CashAccountId = cashAccountId,
                StartUtc = window.StartUtc,
                EndUtc = window.EndUtc
            },
            cancellationToken);
        var items = settlements
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(FinancialSettlementResponse.From)
            .ToArray();
        return PagedResult<FinancialSettlementResponse>.Create(items, resolvedPage, resolvedPageSize, settlements.Count);
    }
}

public sealed class GetCashMovementsUseCase
{
    private readonly ICashMovementRepository _movements;

    public GetCashMovementsUseCase(ICashMovementRepository movements)
    {
        _movements = movements;
    }

    public async Task<PagedResult<CashMovementResponse>> ExecuteAsync(
        Guid? cashAccountId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period: null, DateTime.UtcNow);
        var movements = await _movements.ListAsync(
            new CashMovementListFilter
            {
                CashAccountId = cashAccountId,
                StartUtc = window.StartUtc,
                EndUtc = window.EndUtc
            },
            cancellationToken);
        var items = movements
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(CashMovementResponse.From)
            .ToArray();
        return PagedResult<CashMovementResponse>.Create(items, resolvedPage, resolvedPageSize, movements.Count);
    }
}

internal static class CashBalances
{
    public static decimal Current(decimal openingBalance, IEnumerable<CashMovement> movements) =>
        Summarize(openingBalance, movements).Current;

    public static (decimal Inflows, decimal Outflows, decimal Current) Summarize(
        decimal openingBalance,
        IEnumerable<CashMovement> movements)
    {
        decimal inflows = 0;
        decimal outflows = 0;
        foreach (var movement in movements)
        {
            if (movement.Direction == CashMovementDirection.Inflow)
            {
                inflows += movement.Amount;
            }
            else
            {
                outflows += movement.Amount;
            }
        }

        return (inflows, outflows, openingBalance + inflows - outflows);
    }
}
