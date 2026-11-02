using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Configurations;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class PurchaseReceiptRollbackTests : IDisposable
{
    private readonly string _databasePath;
    private readonly DbContextOptions<InventoryDbContext> _options;
    private readonly InventoryDbContext _dbContext;

    public PurchaseReceiptRollbackTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-po-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        _dbContext = new InventoryDbContext(_options);
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task Receive_WhenMovementFails_PersistsNothing()
    {
        var supplier = Supplier.Create("Acme", null, null, null, null, null);
        var product = Product.Create("sku-roll", "Widget", null, 10m, CategoryConfiguration.UncategorizedCategoryId);
        var balance = InventoryBalance.Create(product.Id);
        var order = PurchaseOrder.Create(
            "PO-20261101-ROLLBACK1",
            supplier.Id,
            supplier.Name,
            null,
            [PurchaseOrderItem.Create(product.Id, product.Sku, product.Name, 20, 1.25m)]);
        order.Send();

        _dbContext.AddRange(supplier, product, balance, order);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var useCase = new ReceivePurchaseOrderUseCase(
            new PurchaseOrderRepository(_dbContext),
            new PurchaseReceiptRepository(_dbContext),
            new SupplierRepository(_dbContext),
            new ProductRepository(_dbContext),
            new ThrowingInventoryRepository(_dbContext),
            new UnitOfWork(_dbContext),
            new NoAudit(),
            new NoUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            order.Id,
            new ReceivePurchaseOrderRequest
            {
                Items =
                [
                    new ReceivePurchaseOrderItemRequest
                    {
                        PurchaseOrderItemId = order.Items.Single().Id,
                        QuantityReceived = 8
                    }
                ]
            }));

        await using var verify = new InventoryDbContext(_options);
        var stored = await verify.PurchaseOrders.Include(item => item.Items).SingleAsync(item => item.Id == order.Id);

        Assert.Equal(PurchaseOrderStatus.Sent, stored.Status);
        Assert.Equal(0, stored.Items.Single().ReceivedQuantity);
        Assert.Empty(verify.PurchaseReceipts);
        Assert.Empty(verify.StockMovements);
        Assert.Equal(0, (await verify.InventoryBalances.SingleAsync()).QuantityOnHand);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        SqliteConnectionClear();
    }

    private void SqliteConnectionClear()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private sealed class ThrowingInventoryRepository : IInventoryRepository
    {
        private readonly InventoryRepository _inner;

        public ThrowingInventoryRepository(InventoryDbContext dbContext)
        {
            _inner = new InventoryRepository(dbContext);
        }

        public Task<InventoryBalance?> GetTrackedByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
            => _inner.GetTrackedByProductIdAsync(productId, cancellationToken);

        public Task<InventoryBalanceDetails?> GetDetailsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
            => _inner.GetDetailsByProductIdAsync(productId, cancellationToken);

        public Task<IReadOnlyList<InventoryBalanceDetails>> GetBalancesAsync(Guid? productId, bool? isLowStock, bool? isActive, CancellationToken cancellationToken = default)
            => _inner.GetBalancesAsync(productId, isLowStock, isActive, cancellationToken);

        public Task AddAsync(InventoryBalance balance, CancellationToken cancellationToken = default)
            => _inner.AddAsync(balance, cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Stock movement failed.");

        public Task<IReadOnlyList<StockMovementDetails>> GetMovementsAsync(StockMovementQuery query, CancellationToken cancellationToken = default)
            => _inner.GetMovementsAsync(query, cancellationToken);
    }

    private sealed class NoAudit : ISecurityAuditWriter
    {
        public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? UserId { get; } = Guid.NewGuid();

        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
    }
}
