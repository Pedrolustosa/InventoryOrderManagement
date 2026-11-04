using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class FinancialSettlementRepository : IFinancialSettlementRepository
{
    private readonly InventoryDbContext _dbContext;

    public FinancialSettlementRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FinancialSettlement?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.FinancialSettlements
            .AsNoTracking()
            .FirstOrDefaultAsync(settlement => settlement.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialSettlement>> ListAsync(
        FinancialSettlementListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var settlements = _dbContext.FinancialSettlements.AsNoTracking().AsQueryable();

        if (filter.Type.HasValue)
        {
            var titleIds = _dbContext.FinancialTitles
                .Where(title => title.Type == filter.Type.Value)
                .Select(title => title.Id);
            settlements = settlements.Where(settlement => titleIds.Contains(settlement.FinancialTitleId));
        }

        if (filter.CashAccountId.HasValue)
        {
            settlements = settlements.Where(settlement => settlement.CashAccountId == filter.CashAccountId.Value);
        }

        if (filter.StartUtc.HasValue)
        {
            settlements = settlements.Where(settlement => settlement.OccurredAtUtc >= filter.StartUtc.Value);
        }

        if (filter.EndUtc.HasValue)
        {
            settlements = settlements.Where(settlement => settlement.OccurredAtUtc < filter.EndUtc.Value);
        }

        return await settlements
            .OrderByDescending(settlement => settlement.OccurredAtUtc)
            .ThenByDescending(settlement => settlement.Id)
            .ToListAsync(cancellationToken);
    }
}
