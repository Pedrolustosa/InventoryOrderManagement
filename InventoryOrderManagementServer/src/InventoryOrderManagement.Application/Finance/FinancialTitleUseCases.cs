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

public sealed class FinancialTitleQuery
{
    public FinancialTitleStatus? Status { get; init; }

    public Guid? CounterpartyId { get; init; }

    public string? DocumentNumber { get; init; }

    public DateOnly? IssueFrom { get; init; }

    public DateOnly? IssueTo { get; init; }

    public DateOnly? DueFrom { get; init; }

    public DateOnly? DueTo { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed class CreatePayableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public CreatePayableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(CreatePayableRequest request, CancellationToken cancellationToken = default) =>
        _editor.CreatePayableAsync(request, cancellationToken);
}

public sealed class CreateReceivableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public CreateReceivableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(CreateReceivableRequest request, CancellationToken cancellationToken = default) =>
        _editor.CreateReceivableAsync(request, cancellationToken);
}

public sealed class UpdatePayableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public UpdatePayableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(Guid id, UpdateFinancialTitleRequest request, CancellationToken cancellationToken = default) =>
        _editor.UpdateAsync(id, FinancialTitleType.Payable, request, cancellationToken);
}

public sealed class UpdateReceivableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public UpdateReceivableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(Guid id, UpdateFinancialTitleRequest request, CancellationToken cancellationToken = default) =>
        _editor.UpdateAsync(id, FinancialTitleType.Receivable, request, cancellationToken);
}

public sealed class CancelPayableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public CancelPayableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(Guid id, CancelFinancialTitleRequest request, CancellationToken cancellationToken = default) =>
        _editor.CancelAsync(id, FinancialTitleType.Payable, request, cancellationToken);
}

public sealed class CancelReceivableUseCase
{
    private readonly FinancialTitleEditor _editor;

    public CancelReceivableUseCase(FinancialTitleEditor editor) => _editor = editor;

    public Task<FinancialTitleResponse> ExecuteAsync(Guid id, CancelFinancialTitleRequest request, CancellationToken cancellationToken = default) =>
        _editor.CancelAsync(id, FinancialTitleType.Receivable, request, cancellationToken);
}

public sealed class GetPayableByIdUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetPayableByIdUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public Task<FinancialTitleResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync(id, FinancialTitleType.Payable, cancellationToken);

    private async Task<FinancialTitleResponse> GetAsync(Guid id, FinancialTitleType type, CancellationToken cancellationToken)
    {
        var title = await _titles.GetByIdAsync(id, cancellationToken) ?? throw new FinancialTitleNotFoundException(id);
        if (title.Type != type)
        {
            throw new FinancialTitleNotFoundException(id);
        }

        return FinancialTitleResponse.From(title, BusinessClock.Today(_time));
    }
}

public sealed class GetReceivableByIdUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetReceivableByIdUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public async Task<FinancialTitleResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var title = await _titles.GetByIdAsync(id, cancellationToken) ?? throw new FinancialTitleNotFoundException(id);
        if (title.Type != FinancialTitleType.Receivable)
        {
            throw new FinancialTitleNotFoundException(id);
        }

        return FinancialTitleResponse.From(title, BusinessClock.Today(_time));
    }
}

public sealed class GetAllPayablesUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetAllPayablesUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public Task<PagedResult<FinancialTitleSummaryResponse>> ExecuteAsync(FinancialTitleQuery? query, CancellationToken cancellationToken = default) =>
        FinancialTitleList.ExecuteAsync(_titles, _time, FinancialTitleType.Payable, query, cancellationToken);
}

public sealed class GetAllReceivablesUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetAllReceivablesUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public Task<PagedResult<FinancialTitleSummaryResponse>> ExecuteAsync(FinancialTitleQuery? query, CancellationToken cancellationToken = default) =>
        FinancialTitleList.ExecuteAsync(_titles, _time, FinancialTitleType.Receivable, query, cancellationToken);
}

public sealed class SettlePayableInstallmentUseCase
{
    private readonly SettleFinancialInstallmentCommand _command;

    public SettlePayableInstallmentUseCase(SettleFinancialInstallmentCommand command) => _command = command;

    public Task<FinancialSettlementResponse> ExecuteAsync(
        Guid titleId,
        Guid installmentId,
        SettleInstallmentRequest request,
        CancellationToken cancellationToken = default) =>
        _command.ExecuteAsync(FinancialTitleType.Payable, titleId, installmentId, request, cancellationToken);
}

public sealed class SettleReceivableInstallmentUseCase
{
    private readonly SettleFinancialInstallmentCommand _command;

    public SettleReceivableInstallmentUseCase(SettleFinancialInstallmentCommand command) => _command = command;

    public Task<FinancialSettlementResponse> ExecuteAsync(
        Guid titleId,
        Guid installmentId,
        SettleInstallmentRequest request,
        CancellationToken cancellationToken = default) =>
        _command.ExecuteAsync(FinancialTitleType.Receivable, titleId, installmentId, request, cancellationToken);
}

internal static class FinancialTitleList
{
    public static async Task<PagedResult<FinancialTitleSummaryResponse>> ExecuteAsync(
        IFinancialTitleRepository titles,
        TimeProvider time,
        FinancialTitleType type,
        FinancialTitleQuery? query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = PageRequest.Normalize(query?.Page, query?.PageSize);
        var stored = await titles.ListAsync(
            new FinancialTitleListFilter
            {
                Type = type,
                CounterpartyId = query?.CounterpartyId,
                DocumentNumber = string.IsNullOrWhiteSpace(query?.DocumentNumber) ? null : query.DocumentNumber.Trim(),
                IssueFrom = query?.IssueFrom,
                IssueTo = query?.IssueTo,
                DueFrom = query?.DueFrom,
                DueTo = query?.DueTo
            },
            cancellationToken);

        var today = BusinessClock.Today(time);
        IEnumerable<FinancialTitle> filtered = stored;
        if (query?.Status is not null)
        {
            filtered = filtered.Where(title => title.StatusOn(today) == query.Status.Value);
        }

        var matches = filtered.ToArray();
        var items = matches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(title => FinancialTitleSummaryResponse.From(title, today))
            .ToArray();
        return PagedResult<FinancialTitleSummaryResponse>.Create(items, page, pageSize, matches.Length);
    }
}
