using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Application.Finance;

public sealed class GetFinanceDashboardUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly ICashAccountRepository _accounts;
    private readonly ICashMovementRepository _movements;
    private readonly TimeProvider _time;

    public GetFinanceDashboardUseCase(
        IFinancialTitleRepository titles,
        ICashAccountRepository accounts,
        ICashMovementRepository movements,
        TimeProvider time)
    {
        _titles = titles;
        _accounts = accounts;
        _movements = movements;
        _time = time;
    }

    public async Task<FinanceDashboardResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var today = BusinessClock.Today(_time);
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period: null, BusinessClock.UtcNow(_time));
        var installments = await _titles.ListInstallmentsAsync(cancellationToken);
        var movements = await _movements.ListAsync(
            new CashMovementListFilter { StartUtc = window.StartUtc, EndUtc = window.EndUtc },
            cancellationToken);
        var accounts = await _accounts.ListAsync(cancellationToken);
        var allMovements = window.StartUtc is null && window.EndUtc is null
            ? movements
            : await _movements.ListAsync(new CashMovementListFilter(), cancellationToken);

        decimal openPayables = 0;
        decimal openReceivables = 0;
        decimal overduePayables = 0;
        decimal overdueReceivables = 0;
        decimal projectedIn = 0;
        decimal projectedOut = 0;

        foreach (var installment in installments)
        {
            if (installment.TitleCancelled || installment.OutstandingBalance <= 0)
            {
                continue;
            }

            var status = FinancialStatus.Installment(
                installment.OutstandingBalance,
                installment.SettledPrincipalAmount,
                installment.DueDate,
                today,
                installment.TitleCancelled);
            var inPeriod = InDateWindow(installment.DueDate, window);
            if (installment.Type == FinancialTitleType.Payable)
            {
                openPayables += installment.OutstandingBalance;
                if (status == FinancialInstallmentStatus.Overdue)
                {
                    overduePayables += installment.OutstandingBalance;
                }

                if (inPeriod)
                {
                    projectedOut += installment.OutstandingBalance;
                }
            }
            else
            {
                openReceivables += installment.OutstandingBalance;
                if (status == FinancialInstallmentStatus.Overdue)
                {
                    overdueReceivables += installment.OutstandingBalance;
                }

                if (inPeriod)
                {
                    projectedIn += installment.OutstandingBalance;
                }
            }
        }

        var (inflows, outflows, _) = CashBalances.Summarize(0, movements);
        return new FinanceDashboardResponse
        {
            OpenPayables = openPayables,
            OpenReceivables = openReceivables,
            OverduePayables = overduePayables,
            OverdueReceivables = overdueReceivables,
            PaidInPeriod = outflows,
            ReceivedInPeriod = inflows,
            RealizedInflows = inflows,
            RealizedOutflows = outflows,
            RealizedNet = inflows - outflows,
            ProjectedInflows = projectedIn,
            ProjectedOutflows = projectedOut,
            CashAccounts = accounts
                .Select(account => CashAccountResponse.From(
                    account,
                    CashBalances.Current(account.OpeningBalance, allMovements.Where(movement => movement.CashAccountId == account.Id))))
                .ToArray()
        };
    }

    private static bool InDateWindow(DateOnly dueDate, ReportWindow window)
    {
        var start = window.StartUtc.HasValue ? DateOnly.FromDateTime(window.StartUtc.Value) : (DateOnly?)null;
        var end = window.EndUtc.HasValue ? DateOnly.FromDateTime(window.EndUtc.Value) : (DateOnly?)null;
        if (start.HasValue && dueDate < start.Value)
        {
            return false;
        }

        if (end.HasValue && dueDate >= end.Value)
        {
            return false;
        }

        return true;
    }
}

public sealed class GetRealizedCashFlowUseCase
{
    private readonly ICashMovementRepository _movements;

    public GetRealizedCashFlowUseCase(ICashMovementRepository movements)
    {
        _movements = movements;
    }

    public async Task<CashFlowResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        Guid? cashAccountId,
        CashFlowGrouping grouping,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period: null, DateTime.UtcNow);
        var movements = await _movements.ListAsync(
            new CashMovementListFilter
            {
                CashAccountId = cashAccountId,
                StartUtc = window.StartUtc,
                EndUtc = window.EndUtc
            },
            cancellationToken);

        var buckets = movements
            .GroupBy(movement => Bucket(DateOnly.FromDateTime(movement.OccurredAtUtc), grouping))
            .OrderBy(group => group.Key)
            .Select(group => BucketResponse(group.Key, group.Where(item => item.Direction == CashMovementDirection.Inflow).Sum(item => item.Amount), group.Where(item => item.Direction == CashMovementDirection.Outflow).Sum(item => item.Amount)))
            .ToArray();

        return Flow(CashFlowNature.Realized, grouping, buckets);
    }

    internal static DateOnly Bucket(DateOnly date, CashFlowGrouping grouping) => grouping switch
    {
        CashFlowGrouping.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        CashFlowGrouping.Month => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };

    internal static CashFlowResponse Flow(CashFlowNature nature, CashFlowGrouping grouping, IReadOnlyList<CashFlowBucketResponse> buckets) => new()
    {
        Nature = nature,
        Grouping = grouping,
        Inflows = buckets.Sum(bucket => bucket.Inflows),
        Outflows = buckets.Sum(bucket => bucket.Outflows),
        Net = buckets.Sum(bucket => bucket.Net),
        Buckets = buckets
    };

    private static CashFlowBucketResponse BucketResponse(DateOnly start, decimal inflows, decimal outflows) => new()
    {
        PeriodStart = start,
        Inflows = inflows,
        Outflows = outflows,
        Net = inflows - outflows
    };
}

public sealed class GetProjectedCashFlowUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetProjectedCashFlowUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public async Task<CashFlowResponse> ExecuteAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CashFlowGrouping grouping,
        CancellationToken cancellationToken = default)
    {
        var window = ReportWindow.Resolve(startDateUtc, endDateUtc, period: null, BusinessClock.UtcNow(_time));
        var installments = await _titles.ListInstallmentsAsync(cancellationToken);
        var open = installments.Where(installment =>
            !installment.TitleCancelled
            && installment.OutstandingBalance > 0
            && InWindow(installment.DueDate, window));

        var buckets = open
            .GroupBy(installment => GetRealizedCashFlowUseCase.Bucket(installment.DueDate, grouping))
            .OrderBy(group => group.Key)
            .Select(group => new CashFlowBucketResponse
            {
                PeriodStart = group.Key,
                Inflows = group.Where(item => item.Type == FinancialTitleType.Receivable).Sum(item => item.OutstandingBalance),
                Outflows = group.Where(item => item.Type == FinancialTitleType.Payable).Sum(item => item.OutstandingBalance),
                Net = group.Where(item => item.Type == FinancialTitleType.Receivable).Sum(item => item.OutstandingBalance)
                    - group.Where(item => item.Type == FinancialTitleType.Payable).Sum(item => item.OutstandingBalance)
            })
            .ToArray();

        return GetRealizedCashFlowUseCase.Flow(CashFlowNature.Projected, grouping, buckets);

        static bool InWindow(DateOnly dueDate, ReportWindow bounds)
        {
            if (bounds.StartUtc.HasValue && dueDate < DateOnly.FromDateTime(bounds.StartUtc.Value))
            {
                return false;
            }

            return !bounds.EndUtc.HasValue || dueDate < DateOnly.FromDateTime(bounds.EndUtc.Value);
        }
    }
}

public sealed class GetFinancialAgingReportUseCase
{
    private readonly IFinancialTitleRepository _titles;
    private readonly TimeProvider _time;

    public GetFinancialAgingReportUseCase(IFinancialTitleRepository titles, TimeProvider time)
    {
        _titles = titles;
        _time = time;
    }

    public async Task<PagedResult<AgingReportItemResponse>> ExecuteAsync(
        FinancialTitleType? type,
        Guid? counterpartyId,
        FinancialInstallmentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        var today = BusinessClock.Today(_time);
        var rows = await _titles.ListInstallmentsAsync(cancellationToken);
        var matches = rows
            .Where(row => !row.TitleCancelled && row.OutstandingBalance > 0)
            .Where(row => type is null || row.Type == type.Value)
            .Where(row => counterpartyId is null || row.CounterpartyId == counterpartyId.Value)
            .Select(row => new AgingReportItemResponse
            {
                TitleId = row.TitleId,
                TitleNumber = row.TitleNumber,
                Type = row.Type,
                InstallmentId = row.InstallmentId,
                InstallmentNumber = row.InstallmentNumber,
                CounterpartyName = row.CounterpartyName,
                CounterpartyType = row.CounterpartyType,
                DocumentNumber = row.DocumentNumber,
                DueDate = row.DueDate,
                DaysOverdue = row.DueDate < today ? today.DayNumber - row.DueDate.DayNumber : 0,
                Amount = row.Amount,
                SettledPrincipalAmount = row.SettledPrincipalAmount,
                OutstandingBalance = row.OutstandingBalance,
                Status = FinancialStatus.Installment(
                    row.OutstandingBalance,
                    row.SettledPrincipalAmount,
                    row.DueDate,
                    today,
                    row.TitleCancelled)
            })
            .Where(item => status is null || item.Status == status.Value)
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.TitleNumber)
            .ToArray();

        var pageItems = matches.Skip((resolvedPage - 1) * resolvedPageSize).Take(resolvedPageSize).ToArray();
        return PagedResult<AgingReportItemResponse>.Create(pageItems, resolvedPage, resolvedPageSize, matches.Length);
    }
}
