using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class ProductRepositoryTests : IDisposable
{
    private readonly string _databasePath;
    private readonly InventoryDbContext _dbContext;
    private readonly ProductRepository _repository;
    private static readonly Guid CategoryId = CategoryConfiguration.UncategorizedCategoryId;

    public ProductRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-infra-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        _dbContext = new InventoryDbContext(options);
        _dbContext.Database.Migrate();
        _repository = new ProductRepository(_dbContext);
    }

    [Fact]
    public async Task Migrate_CreatesProductsTableWithUniqueSkuIndex()
    {
        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT name FROM sqlite_master
            WHERE type = 'index' AND name = 'IX_Products_Sku';
            """;

        var indexName = await command.ExecuteScalarAsync();
        Assert.Equal("IX_Products_Sku", indexName);
    }

    [Fact]
    public async Task AddAsync_PersistsProduct()
    {
        var product = Product.Create("kb-001", "Keyboard", "RGB", 249.90m, CategoryId);

        await _repository.AddAsync(product);

        await using var readContext = CreateContext();
        var stored = await readContext.Products.SingleAsync(item => item.Sku == "KB-001");

        Assert.Equal(product.Id, stored.Id);
        Assert.Equal(CategoryId, stored.CategoryId);
        Assert.Equal("Keyboard", stored.Name);
        Assert.Equal(249.90m, stored.CurrentPrice);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPersistedProducts()
    {
        await _repository.AddAsync(Product.Create("B-2", "Beta", null, 20m, CategoryId));
        await _repository.AddAsync(Product.Create("A-1", "Alpha", null, 10m, CategoryId));

        var products = await _repository.GetAllAsync();

        Assert.Equal(2, products.Count);
        Assert.Equal("Alpha", products[0].Name);
        Assert.Equal("Beta", products[1].Name);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateSku_ThrowsDuplicateSkuException()
    {
        await _repository.AddAsync(Product.Create("KB-001", "Keyboard", null, 100m, CategoryId));

        await using var secondContext = CreateContext();
        var secondRepository = new ProductRepository(secondContext);
        var duplicate = Product.Create("kb-001", "Another Keyboard", null, 50m, CategoryId);

        await Assert.ThrowsAsync<DuplicateSkuException>(() =>
            secondRepository.AddAsync(duplicate));
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
            // Temp SQLite files may remain locked briefly on Windows; safe to ignore in tests.
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
