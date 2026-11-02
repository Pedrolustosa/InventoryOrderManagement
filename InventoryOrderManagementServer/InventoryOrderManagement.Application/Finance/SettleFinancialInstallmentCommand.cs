using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Application.Finance;

public sealed class SettleFinancialInstallmentCommand
{
    private readonly IFinancialTitleRepository _titles;
    private readonly IFinancialSettlementRepository _settlements;
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _audit;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public SettleFinancialInstallmentCommand(
        IFinancialTitleRepository titles,
        IFinancialSettlementRepository settlements,
        ICashAccountRepository accounts,
        ICashMovementRepository movements,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter audit,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _titles = titles;
        _settlements = settlements;
        _accounts = accounts;
        _movements = movements;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<FinancialSettlementResponse> ExecuteAsync(
        FinancialTitleType expectedType,
        Guid titleId,
        Guid installmentId,
        SettleInstallmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        var replay = await FindReplayAsync(key, titleId, installmentId, request.SettledPrincipalAmount, cancellationToken);
        if (replay is not null)
        {
            return FinancialSettlementResponse.From(replay);
        }

        FinancialSettlement? created = null;
        var recorded = false;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var raced = await FindReplayAsync(key, titleId, installmentId, request.SettledPrincipalAmount, ct);
            if (raced is not null)
            {
                created = raced;
                return;
            }

            var title = await _titles.GetTrackedByIdAsync(titleId, ct) ?? throw new FinancialTitleNotFoundException(titleId);
            if (title.Type != expectedType)
            {
                throw new FinancialConflictException(
                    expectedType == FinancialTitleType.Payable
                        ? "A receivable cannot be settled as a payment."
                        : "A payable cannot be settled as a receipt.");
            }

            if (title.Installments.All(installment => installment.Id != installmentId))
            {
                throw new FinancialInstallmentNotFoundException(installmentId);
            }

            await EnsureAccountAsync(request, ct);
            var result = title.SettleInstallment(
                installmentId,
                request.CashAccountId,
                request.PaymentMethod,
                request.SettledPrincipalAmount,
                request.DiscountAmount,
                request.InterestAmount,
                request.FineAmount,
                request.OccurredAtUtc,
                request.Reference,
                request.Notes,
                key,
                BusinessClock.UtcNow(_time),
                _currentUser.UserId);

            _titles.StageSettlement(result.Settlement);
            if (result.Movement is not null)
            {
                await _movements.AddAsync(result.Movement, ct);
            }

            created = result.Settlement;
            recorded = true;
        }, cancellationToken);

        var settlement = created ?? throw new InvalidOperationException("Settlement was not recorded.");
        if (recorded)
        {
            await FinanceAudit.WriteAsync(
                _audit,
                _currentUser,
                SecurityActions.FinancialSettlementCreated,
                "FinancialSettlement",
                settlement.Id,
                $"Title {settlement.FinancialTitleId}; installment {settlement.FinancialInstallmentId}; account {settlement.CashAccountId}",
                cancellationToken);
        }

        return FinancialSettlementResponse.From(settlement);
    }

    private async Task EnsureAccountAsync(SettleInstallmentRequest request, CancellationToken cancellationToken)
    {
        var principal = Money.Require(request.SettledPrincipalAmount, "Settled principal amount");
        var discount = Money.Require(request.DiscountAmount, "Discount amount", allowZero: true);
        var interest = Money.Require(request.InterestAmount, "Interest amount", allowZero: true);
        var fine = Money.Require(request.FineAmount, "Fine amount", allowZero: true);
        var cashAmount = Money.Round(principal - discount + interest + fine);
        if (cashAmount <= 0)
        {
            return;
        }

        if (request.CashAccountId is null)
        {
            throw new DomainException("An active cash account is required when cash is moved.");
        }

        var account = await _accounts.GetByIdAsync(request.CashAccountId.Value, cancellationToken)
            ?? throw new CashAccountNotFoundException(request.CashAccountId.Value);
        if (!account.IsActive)
        {
            throw new InactiveCashAccountException(account.Id);
        }
    }

    private async Task<FinancialSettlement?> FindReplayAsync(
        string? key,
        Guid titleId,
        Guid installmentId,
        decimal principal,
        CancellationToken cancellationToken)
    {
        if (key is null)
        {
            return null;
        }

        var existing = await _settlements.FindByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (existing.FinancialTitleId != titleId
            || existing.FinancialInstallmentId != installmentId
            || existing.SettledPrincipalAmount != principal)
        {
            throw new FinancialConflictException("The idempotency key was already used for a different settlement.");
        }

        return existing;
    }
}
