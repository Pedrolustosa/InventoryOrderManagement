using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class CategoryRepositoryTests : IDisposable
{
    private readonly string _databasePath;
    private readonly InventoryDbContext _dbContext;
    private readonly CategoryRepository _categoryRepository;
    private readonly ProductRepository _productRepository;

    public CategoryRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-cat-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        _dbContext = new InventoryDbContext(options);
        _dbContext.Database.Migrate();
        _categoryRepository = new CategoryRepository(_dbContext);
        _productRepository = new ProductRepository(_dbContext);
    }

    [Fact]
    public async Task Migrate_SeedsUncategorizedCategory()
    {
        var category = await _categoryRepository.GetByIdAsync(CategoryConfiguration.UncategorizedCategoryId);

        Assert.NotNull(category);
        Assert.Equal("Uncategorized", category.Name);
        Assert.Equal("UNCATEGORIZED", category.NormalizedName);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateNormalizedName_Throws()
    {
        await _categoryRepository.AddAsync(Category.Create("Electronics", null));

        await using var secondContext = CreateContext();
        var secondRepository = new CategoryRepository(secondContext);

        await Assert.ThrowsAsync<DuplicateCategoryNameException>(() =>
            secondRepository.AddAsync(Category.Create("electronics", null)));
    }

    [Fact]
    public async Task HasActiveProductsAsync_ReturnsTrueWhenProductExists()
    {
        var category = Category.Create("Peripherals", null);
        await _categoryRepository.AddAsync(category);
        await _productRepository.AddAsync(
            Product.Create("KB-1", "Keyboard", null, 10m, category.Id));

        var hasActive = await _categoryRepository.HasActiveProductsAsync(category.Id);

        Assert.True(hasActive);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        SqliteConnection.ClearAllPools();

        TryDelete(_databasePath);
        TryDelete(_databasePath + "-shm");
        TryDelete(_databasePath + "-wal");

        GC.SuppressFinalize(this);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }

    private InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new InventoryDbContext(options);
    }
}
