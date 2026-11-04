using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class SupplierRepositoryTests : IDisposable
{
    private readonly string _databasePath;
    private readonly InventoryDbContext _dbContext;
    private readonly SupplierRepository _repository;

    public SupplierRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-sup-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        _dbContext = new InventoryDbContext(options);
        _dbContext.Database.Migrate();
        _repository = new SupplierRepository(_dbContext);
    }

    [Fact]
    public async Task Migrate_CreatesSuppliersTable()
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name='Suppliers';";

        var tableName = await command.ExecuteScalarAsync();
        Assert.Equal("Suppliers", tableName);
    }

    [Fact]
    public async Task AddAndGet_PersistsSupplier()
    {
        var supplier = Supplier.Create(
            "Tech Components",
            "12.345.678/0001-90",
            "John",
            "a@b.com",
            "123",
            "Notes");

        await _repository.AddAsync(supplier);

        var loaded = await _repository.GetByIdAsync(supplier.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Tech Components", loaded.Name);
        Assert.Equal("12345678000190", loaded.NormalizedTaxIdentifier);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByNameAndIsActive()
    {
        await _repository.AddAsync(Supplier.Create("Alpha Tech", null, null, null, null, null));
        var inactive = Supplier.Create("Beta Soft", null, null, null, null, null);
        await _repository.AddAsync(inactive);
        inactive.Deactivate();
        await _repository.UpdateAsync(inactive);
        await _repository.AddAsync(Supplier.Create("Gamma Parts", null, null, null, null, null));

        var activeTech = await _repository.GetAllAsync(isActive: true, name: "tech");

        Assert.Single(activeTech);
        Assert.Equal("Alpha Tech", activeTech[0].Name);
    }

    [Fact]
    public async Task AddAsync_AllowsMultipleNullTaxIdentifiers()
    {
        await _repository.AddAsync(Supplier.Create("One", null, null, null, null, null));
        await _repository.AddAsync(Supplier.Create("Two", null, null, null, null, null));

        var all = await _repository.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateNormalizedTaxIdentifier_Throws()
    {
        await _repository.AddAsync(Supplier.Create("One", "12.345.678/0001-90", null, null, null, null));

        await using var secondContext = CreateContext();
        var secondRepository = new SupplierRepository(secondContext);

        await Assert.ThrowsAsync<DuplicateTaxIdentifierException>(() =>
            secondRepository.AddAsync(Supplier.Create("Two", "12345678000190", null, null, null, null)));
    }

    [Fact]
    public async Task UpdateAndDeactivate_PersistChanges()
    {
        var supplier = Supplier.Create("Old", null, null, null, null, null);
        await _repository.AddAsync(supplier);

        supplier.Update("New", "999", "Contact", "a@b.com", "1", "N");
        await _repository.UpdateAsync(supplier);

        supplier.Deactivate();
        await _repository.UpdateAsync(supplier);

        var loaded = await _repository.GetByIdAsync(supplier.Id);
        Assert.NotNull(loaded);
        Assert.Equal("New", loaded.Name);
        Assert.False(loaded.IsActive);
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
