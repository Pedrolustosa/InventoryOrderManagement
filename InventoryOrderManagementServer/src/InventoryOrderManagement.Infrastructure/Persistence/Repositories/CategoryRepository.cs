using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly InventoryDbContext _dbContext;

    public CategoryRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.NormalizedName == normalizedName);

        if (excludeId.HasValue)
        {
            query = query.Where(category => category.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(category => category.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCategoryNameException(category.Name);
        }
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _dbContext.Categories.Update(category);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCategoryNameException(category.Name);
        }
    }

    public Task<bool> HasActiveProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.CategoryId == categoryId && product.IsActive,
                cancellationToken);
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
