using System.Data.Common;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using InventoryOrderManagement.Infrastructure.Persistence.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

/// <summary>Proves the order timeline runs a fixed number of queries (no N+1), whatever its size.</summary>
public class OrderTimelineQueryCountTests : IDisposable
{
    private readonly string _databasePath;
    private readonly DbContextOptions<InventoryDbContext> _options;

    public OrderTimelineQueryCountTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-timeline-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        using var dbContext = new InventoryDbContext(_options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task LoadAsync_RunsTheSameNumberOfQueries_ForSmallAndLargeTimelines()
    {
        var small = await SeedOrderAsync("SMALL", size: 1);
        var large = await SeedOrderAsync("LARGE", size: 40);

        var (smallSources, smallQueries) = await LoadCountingQueriesAsync(small);
        var (largeSources, largeQueries) = await LoadCountingQueriesAsync(large);

        Assert.Equal(1, smallSources!.Movements.Count);
        Assert.Equal(40, largeSources!.Movements.Count);
        Assert.Equal(40, largeSources.Settlements.Count);
        Assert.Equal(40, largeSources.Returns.Count);
        Assert.Equal(smallQueries, largeQueries);
        Assert.InRange(largeQueries, 1, 7);
    }

    [Fact]
    public async Task LoadAsync_WithoutFinanceAndReturns_SkipsThoseQueries()
    {
        var orderId = await SeedOrderAsync("NOFIN", size: 5);

        var (_, allQueries) = await LoadCountingQueriesAsync(orderId);
        var (sources, restrictedQueries) = await LoadCountingQueriesAsync(orderId, includeFinance: false, includeReturns: false);

        Assert.Empty(sources!.Titles);
        Assert.Empty(sources.Settlements);
        Assert.Empty(sources.Returns);
        Assert.True(restrictedQueries < allQueries);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
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

        GC.SuppressFinalize(this);
    }

    private async Task<(Application.Abstractions.Orders.OrderTimelineSources? Sources, int Queries)> LoadCountingQueriesAsync(
        Guid orderId,
        bool includeFinance = true,
        bool includeReturns = true)
    {
        var counter = new CommandCounter();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .AddInterceptors(counter)
            .Options;

        await using var dbContext = new InventoryDbContext(options);
        var sources = await new OrderTimelineQuery(dbContext).LoadAsync(orderId, includeFinance, includeReturns);
        return (sources, counter.Count);
    }

    /// <summary>
    /// An order whose timeline grows with <paramref name="size"/>: that many reservations, receivable
    /// installments with their settlements, and returns.
    /// </summary>
    private async Task<Guid> SeedOrderAsync(string key, int size)
    {
        await using var dbContext = new InventoryDbContext(_options);

        var customer = Customer.Create(CustomerType.Individual, $"Customer {key}", null, null, null, null);
        var product = Product.Create($"SKU-{key}", $"Product {key}", null, 10m, CategoryConfiguration.UncategorizedCategoryId);
        var order = Order.Create(
            $"ORD-{key}",
            customer.Id,
            customer.Name,
            OrderShippingAddress.Create("00000-000", "Street", "1", null, null, "City", "ST", "BR"),
            [OrderItem.Create(product.Id, product.Sku, product.Name, size, 1m)]);
        order.Confirm();

        var shipment = Shipment.Create(order.Id, order.OrderNumber, customer.Id, customer.Name, "Acme", null, null, null);

        var movements = Enumerable.Range(0, size)
            .Select(index => StockMovement.CreateForOrder(
                product.Id,
                order.Id,
                StockMovementType.OrderReservation,
                1,
                index,
                index + 1,
                "Reserved"))
            .ToList();

        var installments = Enumerable.Range(1, size)
            .Select(number => (number, new DateOnly(2026, 12, 1).AddDays(number), 1m))
            .ToList();
        var receivable = FinancialTitle.Create(
            $"FT-{key}",
            FinancialTitleType.Receivable,
            "Receivable",
            FinancialCounterpartyType.Customer,
            customer.Id,
            customer.Name,
            FinancialSourceType.SalesOrder,
            order.Id,
            order.OrderNumber,
            null,
            new DateOnly(2026, 11, 1),
            size,
            null,
            null,
            installments,
            DateTime.UtcNow);
        // One cash-less settlement per installment.
        receivable.ApplyReturnCredit(size - 0.5m, Guid.NewGuid(), $"RET-{key}", DateTime.UtcNow);

        var orderItemId = order.Items.Single().Id;
        var returns = Enumerable.Range(0, size)
            .Select(index => ReturnRequest.CreateCustomer(
                $"RET-{key}-{index}",
                customer.Id,
                customer.Name,
                order.Id,
                order.OrderNumber,
                ReturnReason.Defective,
                null,
                null,
                ReturnFinancialTreatment.None,
                null,
                null,
                [ReturnItem.CreateForCustomer(product.Id, product.Sku, product.Name, orderItemId, size, 0, 1, 1m)]))
            .ToList();

        dbContext.AddRange(customer, product, order, shipment, receivable);
        dbContext.AddRange(movements);
        dbContext.AddRange(returns);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
