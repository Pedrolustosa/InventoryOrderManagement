using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class FinancialTitleRepository : IFinancialTitleRepository
{
    private readonly InventoryDbContext _dbContext;

    public FinancialTitleRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(FinancialTitle title, CancellationToken cancellationToken = default)
    {
        await _dbContext.FinancialTitles.AddAsync(title, cancellationToken);
    }

    public Task<FinancialTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Query().AsNoTracking().FirstOrDefaultAsync(title => title.Id == id, cancellationToken);
    }

    public Task<FinancialTitle?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Query().FirstOrDefaultAsync(title => title.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNumberAsync(string titleNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.FinancialTitles.AnyAsync(title => title.TitleNumber == titleNumber, cancellationToken);
    }

    public Task<bool> DocumentExistsAsync(FinancialTitle title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title.DocumentNumber))
        {
            return Task.FromResult(false);
        }

        var query = _dbContext.FinancialTitles.Where(item =>
            item.Id != title.Id
            && item.Type == title.Type
            && item.DocumentNumber == title.DocumentNumber);

        query = title.CounterpartyId.HasValue
            ? query.Where(item => item.CounterpartyId == title.CounterpartyId)
            : query.Where(item => item.CounterpartyId == null && item.CounterpartyName == title.CounterpartyName);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialTitle>> ListAsync(
        FinancialTitleListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var titles = _dbContext.FinancialTitles.AsNoTracking().Include(title => title.Installments).AsQueryable();
        titles = titles.Where(title => title.Type == filter.Type);

        if (filter.CounterpartyId.HasValue)
        {
            titles = titles.Where(title => title.CounterpartyId == filter.CounterpartyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DocumentNumber))
        {
            titles = titles.Where(title => title.DocumentNumber == filter.DocumentNumber);
        }

        if (filter.IssueFrom.HasValue)
        {
            titles = titles.Where(title => title.IssueDate >= filter.IssueFrom.Value);
        }

        if (filter.IssueTo.HasValue)
        {
            titles = titles.Where(title => title.IssueDate <= filter.IssueTo.Value);
        }

        if (filter.DueFrom.HasValue)
        {
            titles = titles.Where(title => title.Installments.Any(installment => installment.DueDate >= filter.DueFrom.Value));
        }

        if (filter.DueTo.HasValue)
        {
            titles = titles.Where(title => title.Installments.Any(installment => installment.DueDate <= filter.DueTo.Value));
        }

        return await titles
            .OrderByDescending(title => title.IssueDate)
            .ThenByDescending(title => title.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialInstallmentRow>> ListInstallmentsAsync(CancellationToken cancellationToken = default)
    {
        return await (
                from installment in _dbContext.FinancialInstallments.AsNoTracking()
                join title in _dbContext.FinancialTitles.AsNoTracking()
                    on installment.FinancialTitleId equals title.Id
                select new FinancialInstallmentRow
                {
                    TitleId = title.Id,
                    TitleNumber = title.TitleNumber,
                    Type = title.Type,
                    CounterpartyType = title.CounterpartyType,
                    CounterpartyId = title.CounterpartyId,
                    CounterpartyName = title.CounterpartyName,
                    DocumentNumber = title.DocumentNumber,
                    IssueDate = title.IssueDate,
                    TitleCancelled = title.CancelledAtUtc != null,
                    InstallmentId = installment.Id,
                    InstallmentNumber = installment.InstallmentNumber,
                    DueDate = installment.DueDate,
                    Amount = installment.Amount,
                    SettledPrincipalAmount = installment.SettledPrincipalAmount,
                    OutstandingBalance = installment.OutstandingBalance
                })
            .ToListAsync(cancellationToken);
    }

    public void RemoveInstallments(IReadOnlyCollection<FinancialInstallment> installments)
    {
        _dbContext.FinancialInstallments.RemoveRange(installments);
    }

    public void StageSettlement(FinancialSettlement settlement)
    {
        if (_dbContext.Entry(settlement).State == EntityState.Detached)
        {
            _dbContext.FinancialSettlements.Add(settlement);
        }
    }

    private IQueryable<FinancialTitle> Query() =>
        _dbContext.FinancialTitles
            .Include(title => title.Installments)
            .Include(title => title.Settlements);
}
