using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly InventoryDbContext _dbContext;

    public CustomerRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNormalizedTaxIdentifierAsync(
        string normalizedTaxIdentifier,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.NormalizedTaxIdentifier == normalizedTaxIdentifier);

        if (excludeId.HasValue)
        {
            query = query.Where(customer => customer.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        bool? isActive = null,
        string? name = null,
        CustomerType? customerType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == isActive.Value);
        }

        if (customerType.HasValue)
        {
            query = query.Where(customer => customer.CustomerType == customerType.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLowerInvariant();
            query = query.Where(customer => customer.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _dbContext.Customers.AddAsync(customer, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCustomerTaxIdentifierException(
                customer.TaxIdentifier ?? customer.NormalizedTaxIdentifier!);
        }
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _dbContext.Customers.Update(customer);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCustomerTaxIdentifierException(
                customer.TaxIdentifier ?? customer.NormalizedTaxIdentifier!);
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
