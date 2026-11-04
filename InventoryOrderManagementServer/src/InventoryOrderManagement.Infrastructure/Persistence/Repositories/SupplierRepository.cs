using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly InventoryDbContext _dbContext;

    public SupplierRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Suppliers
            .AsNoTracking()
            .AnyAsync(supplier => supplier.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNormalizedTaxIdentifierAsync(
        string normalizedTaxIdentifier,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.NormalizedTaxIdentifier == normalizedTaxIdentifier);

        if (excludeId.HasValue)
        {
            query = query.Where(supplier => supplier.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Suppliers
            .FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(
        bool? isActive = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(supplier => supplier.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLowerInvariant();
            query = query.Where(supplier => supplier.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(supplier => supplier.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateTaxIdentifierException(supplier.TaxIdentifier ?? supplier.NormalizedTaxIdentifier!);
        }
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _dbContext.Suppliers.Update(supplier);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateTaxIdentifierException(supplier.TaxIdentifier ?? supplier.NormalizedTaxIdentifier!);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqliteException sqliteException)
        {
            return sqliteException.SqliteExtendedErrorCode is 2067
                || (sqliteException.SqliteErrorCode == 19
                    && sqliteException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
        }

        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
