using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Finance;

public sealed class FinancialTitleEditor
{
    private const int MaxNumberAttempts = 5;

    private readonly IFinancialTitleRepository _titles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinancialPartyResolver _parties;
    private readonly ISecurityAuditWriter _audit;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public FinancialTitleEditor(
        IFinancialTitleRepository titles,
        IUnitOfWork unitOfWork,
        FinancialPartyResolver parties,
        ISecurityAuditWriter audit,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _titles = titles;
        _unitOfWork = unitOfWork;
        _parties = parties;
        _audit = audit;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<FinancialTitleResponse> CreatePayableAsync(
        CreatePayableRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var party = await _parties.ForPayableAsync(
            request.SupplierId,
            request.CounterpartyName,
            request.SourceType,
            request.SourceId,
            cancellationToken);
        return await CreateAsync(
            FinancialTitleType.Payable,
            request.Description,
            party,
            request.DocumentNumber,
            request.IssueDate,
            request.TotalAmount,
            request.Notes,
            request.Installments,
            cancellationToken);
    }

    public async Task<FinancialTitleResponse> CreateReceivableAsync(
        CreateReceivableRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var party = await _parties.ForReceivableAsync(
            request.CustomerId,
            request.CounterpartyName,
            request.SourceType,
            request.SourceId,
            cancellationToken);
        return await CreateAsync(
            FinancialTitleType.Receivable,
            request.Description,
            party,
            request.DocumentNumber,
            request.IssueDate,
            request.TotalAmount,
            request.Notes,
            request.Installments,
            cancellationToken);
    }

    public async Task<FinancialTitleResponse> UpdateAsync(
        Guid id,
        FinancialTitleType expectedType,
        UpdateFinancialTitleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _titles.GetByIdAsync(id, cancellationToken)
            ?? throw new FinancialTitleNotFoundException(id);
        if (existing.Type != expectedType)
        {
            throw new FinancialConflictException("The financial title type does not match this operation.");
        }

        var party = expectedType == FinancialTitleType.Payable
            ? await _parties.ForPayableAsync(request.CounterpartyId, request.CounterpartyName, request.SourceType, request.SourceId, cancellationToken)
            : await _parties.ForReceivableAsync(request.CounterpartyId, request.CounterpartyName, request.SourceType, request.SourceId, cancellationToken);

        FinancialTitle? updated = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var title = await _titles.GetTrackedByIdAsync(id, ct) ?? throw new FinancialTitleNotFoundException(id);
            var removed = title.Update(
                request.Description,
                party.CounterpartyType,
                party.CounterpartyId,
                party.CounterpartyName,
                party.SourceType,
                party.SourceId,
                party.SourceReference,
                request.DocumentNumber,
                request.IssueDate,
                request.TotalAmount,
                request.Notes,
                Lines(request.Installments),
                BusinessClock.UtcNow(_time));
            await EnsureDocumentAvailableAsync(title, ct);
            _titles.RemoveInstallments(removed);
            updated = title;
        }, cancellationToken);

        var stored = updated ?? throw new InvalidOperationException("Financial title was not updated.");
        await FinanceAudit.WriteAsync(
            _audit,
            _currentUser,
            SecurityActions.FinancialTitleUpdated,
            "FinancialTitle",
            stored.Id,
            stored.TitleNumber,
            cancellationToken);
        return FinancialTitleResponse.From(stored, BusinessClock.Today(_time));
    }

    public async Task<FinancialTitleResponse> CancelAsync(
        Guid id,
        FinancialTitleType expectedType,
        CancelFinancialTitleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        FinancialTitle? cancelled = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var title = await _titles.GetTrackedByIdAsync(id, ct) ?? throw new FinancialTitleNotFoundException(id);
            if (title.Type != expectedType)
            {
                throw new FinancialConflictException("The financial title type does not match this operation.");
            }

            title.Cancel(request.Reason, BusinessClock.UtcNow(_time));
            cancelled = title;
        }, cancellationToken);

        var stored = cancelled ?? throw new InvalidOperationException("Financial title was not cancelled.");
        await FinanceAudit.WriteAsync(
            _audit,
            _currentUser,
            SecurityActions.FinancialTitleCancelled,
            "FinancialTitle",
            stored.Id,
            stored.CancellationReason,
            cancellationToken);
        return FinancialTitleResponse.From(stored, BusinessClock.Today(_time));
    }

    private async Task<FinancialTitleResponse> CreateAsync(
        FinancialTitleType type,
        string description,
        ResolvedParty party,
        string? documentNumber,
        DateOnly issueDate,
        decimal totalAmount,
        string? notes,
        IReadOnlyList<FinancialInstallmentRequest>? installments,
        CancellationToken cancellationToken)
    {
        FinancialTitle? created = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var title = FinancialTitle.Create(
                await NextNumberAsync(ct),
                type,
                description,
                party.CounterpartyType,
                party.CounterpartyId,
                party.CounterpartyName,
                party.SourceType,
                party.SourceId,
                party.SourceReference,
                documentNumber,
                issueDate,
                totalAmount,
                notes,
                _currentUser.UserId,
                Lines(installments),
                BusinessClock.UtcNow(_time));
            await EnsureDocumentAvailableAsync(title, ct);
            await _titles.AddAsync(title, ct);
            created = title;
        }, cancellationToken);

        var stored = created ?? throw new InvalidOperationException("Financial title was not created.");
        await FinanceAudit.WriteAsync(
            _audit,
            _currentUser,
            SecurityActions.FinancialTitleCreated,
            "FinancialTitle",
            stored.Id,
            stored.TitleNumber,
            cancellationToken);
        return FinancialTitleResponse.From(stored, BusinessClock.Today(_time));
    }

    private async Task EnsureDocumentAvailableAsync(FinancialTitle title, CancellationToken cancellationToken)
    {
        if (await _titles.DocumentExistsAsync(title, cancellationToken))
        {
            throw new DuplicateFinancialDocumentException(title.DocumentNumber!);
        }
    }

    private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxNumberAttempts; attempt++)
        {
            var candidate = $"FT-{BusinessClock.UtcNow(_time):yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (!await _titles.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException("Financial title number could not be generated.");
    }

    private static IReadOnlyList<(int InstallmentNumber, DateOnly DueDate, decimal Amount)> Lines(
        IReadOnlyList<FinancialInstallmentRequest>? installments) =>
        (installments ?? [])
            .Select(installment => (installment.InstallmentNumber, installment.DueDate, installment.Amount))
            .ToArray();
}
