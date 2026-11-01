using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class CustomerRepositoryTests : IDisposable
{
    private readonly string _databasePath;
    private readonly InventoryDbContext _dbContext;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-cust-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        _dbContext = new InventoryDbContext(options);
        _dbContext.Database.Migrate();
        _repository = new CustomerRepository(_dbContext);
    }

    [Fact]
    public async Task Migrate_CreatesCustomersTable()
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name='Customers';";

        var tableName = await command.ExecuteScalarAsync();
        Assert.Equal("Customers", tableName);
    }

    [Fact]
    public async Task AddAndGet_PersistsCustomerWithAddress()
    {
        var address = CustomerAddress.Create(
            "50000-000",
            "Example Street",
            "100",
            "Apt 1",
            "Boa Viagem",
            "Recife",
            "PE",
            "Brazil");

        var customer = Customer.Create(
            CustomerType.Individual,
            "John Smith",
            "123.456.789-00",
            "john@example.com",
            "123",
            address);

        await _repository.AddAsync(customer);

        var loaded = await _repository.GetByIdAsync(customer.Id);

        Assert.NotNull(loaded);
        Assert.Equal("John Smith", loaded.Name);
        Assert.Equal("12345678900", loaded.NormalizedTaxIdentifier);
        Assert.NotNull(loaded.Address);
        Assert.Equal("Recife", loaded.Address.City);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByNameTypeAndIsActive()
    {
        await _repository.AddAsync(
            Customer.Create(CustomerType.Individual, "Alpha Person", null, null, null, null));

        var inactive = Customer.Create(CustomerType.Company, "Beta Corp", null, null, null, null);
        await _repository.AddAsync(inactive);
        inactive.Deactivate();
        await _repository.UpdateAsync(inactive);

        await _repository.AddAsync(
            Customer.Create(CustomerType.Individual, "Gamma Guest", null, null, null, null));

        var filtered = await _repository.GetAllAsync(
            isActive: true,
            name: "alpha",
            customerType: CustomerType.Individual);

        Assert.Single(filtered);
        Assert.Equal("Alpha Person", filtered[0].Name);
    }

    [Fact]
    public async Task AddAsync_AllowsMultipleNullTaxIdentifiers()
    {
        await _repository.AddAsync(Customer.Create(CustomerType.Individual, "One", null, null, null, null));
        await _repository.AddAsync(Customer.Create(CustomerType.Individual, "Two", null, null, null, null));

        var all = await _repository.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateNormalizedTaxIdentifier_Throws()
    {
        await _repository.AddAsync(
            Customer.Create(CustomerType.Individual, "One", "123.456.789-00", null, null, null));

        await using var secondContext = CreateContext();
        var secondRepository = new CustomerRepository(secondContext);

        await Assert.ThrowsAsync<DuplicateCustomerTaxIdentifierException>(() =>
            secondRepository.AddAsync(
                Customer.Create(CustomerType.Individual, "Two", "12345678900", null, null, null)));
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
