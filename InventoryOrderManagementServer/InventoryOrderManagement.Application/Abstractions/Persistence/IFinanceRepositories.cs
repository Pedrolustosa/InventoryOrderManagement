using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IFinancialTitleRepository
{
    Task AddAsync(FinancialTitle title, CancellationToken cancellationToken = default);

    Task<FinancialTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FinancialTitle?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(string titleNumber, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(
        FinancialTitle title,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialTitle>> ListAsync(
        FinancialTitleListFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialInstallmentRow>> ListInstallmentsAsync(CancellationToken cancellationToken = default);

    void RemoveInstallments(IReadOnlyCollection<FinancialInstallment> installments);

    void StageSettlement(FinancialSettlement settlement);
}

public interface IFinancialSettlementRepository
{
    Task<FinancialSettlement?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialSettlement>> ListAsync(
        FinancialSettlementListFilter filter,
        CancellationToken cancellationToken = default);
}

public interface ICashAccountRepository
{
    Task AddAsync(CashAccount account, CancellationToken cancellationToken = default);

    Task<CashAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CashAccount?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashAccount>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> HasMovementsAsync(Guid cashAccountId, CancellationToken cancellationToken = default);
}

public interface ICashMovementRepository
{
    Task AddAsync(CashMovement movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashMovement>> ListAsync(
        CashMovementListFilter filter,
        CancellationToken cancellationToken = default);
}
