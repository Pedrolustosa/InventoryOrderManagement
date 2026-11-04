using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Persistence;

public class FinanceSettlementTests : IDisposable
{
    private readonly string _databasePath;
    private readonly DbContextOptions<InventoryDbContext> _options;
    private readonly InventoryDbContext _dbContext;

    public FinanceSettlementTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-finance-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        _dbContext = new InventoryDbContext(_options);
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task Balance_AfterInflowAndOutflow_IsOpeningPlusNet()
    {
        var balance = new GetCashAccountBalanceUseCase(
            new CashAccountRepository(_dbContext),
            new CashMovementRepository(_dbContext));
        var editor = Editor(_dbContext);
        var account = await new CreateCashAccountUseCase(
            new CashAccountRepository(_dbContext),
            new UnitOfWork(_dbContext),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System).ExecuteAsync(new CreateCashAccountRequest
        {
            Name = "Main cash",
            AccountType = CashAccountType.Cash,
            OpeningBalance = 1000m,
            OpeningBalanceDate = new DateOnly(2026, 11, 1)
        });

        var receivable = await editor.CreateReceivableAsync(ManualReceivable(500m), CancellationToken.None);
        await Settle(_dbContext).ExecuteAsync(
            FinancialTitleType.Receivable,
            receivable.Id,
            receivable.Installments.Single().Id,
            Settlement(account.Id, 500m),
            CancellationToken.None);

        var payable = await editor.CreatePayableAsync(ManualPayable(200m), CancellationToken.None);
        await Settle(_dbContext).ExecuteAsync(
            FinancialTitleType.Payable,
            payable.Id,
            payable.Installments.Single().Id,
            Settlement(account.Id, 200m),
            CancellationToken.None);

        var current = await balance.ExecuteAsync(account.Id);
        Assert.Equal(500m, current.TotalInflows);
        Assert.Equal(200m, current.TotalOutflows);
        Assert.Equal(1300m, current.CurrentBalance);
    }

    [Fact]
    public async Task Settle_WhenMovementFails_PersistsNothing()
    {
        var editor = Editor(_dbContext);
        var account = await new CreateCashAccountUseCase(
            new CashAccountRepository(_dbContext),
            new UnitOfWork(_dbContext),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System).ExecuteAsync(new CreateCashAccountRequest
        {
            Name = "Main cash",
            AccountType = CashAccountType.CheckingAccount,
            OpeningBalance = 0m,
            OpeningBalanceDate = new DateOnly(2026, 11, 1)
        });
        var payable = await editor.CreatePayableAsync(ManualPayable(1000m), CancellationToken.None);
        _dbContext.ChangeTracker.Clear();

        var command = new SettleFinancialInstallmentCommand(
            new FinancialTitleRepository(_dbContext),
            new FinancialSettlementRepository(_dbContext),
            new CashAccountRepository(_dbContext),
            new ThrowingMovementRepository(),
            new UnitOfWork(_dbContext),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync(
            FinancialTitleType.Payable,
            payable.Id,
            payable.Installments.Single().Id,
            Settlement(account.Id, 400m),
            CancellationToken.None));

        await using var verify = new InventoryDbContext(_options);
        var installment = await verify.FinancialInstallments.SingleAsync();
        Assert.Equal(1000m, installment.OutstandingBalance);
        Assert.Equal(0m, installment.SettledPrincipalAmount);
        Assert.Empty(verify.FinancialSettlements);
        Assert.Empty(verify.CashMovements);
    }

    [Fact]
    public async Task Settle_StaleConcurrentCopy_DoesNotExceedTheBalance()
    {
        var editor = Editor(_dbContext);
        var account = await new CreateCashAccountUseCase(
            new CashAccountRepository(_dbContext),
            new UnitOfWork(_dbContext),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System).ExecuteAsync(new CreateCashAccountRequest
        {
            Name = "Main cash",
            AccountType = CashAccountType.Cash,
            OpeningBalance = 0m,
            OpeningBalanceDate = new DateOnly(2026, 11, 1)
        });
        var payable = await editor.CreatePayableAsync(ManualPayable(1000m), CancellationToken.None);
        var installmentId = payable.Installments.Single().Id;

        await using var first = new InventoryDbContext(_options);
        await using var second = new InventoryDbContext(_options);
        var left = await new FinancialTitleRepository(first).GetTrackedByIdAsync(payable.Id);
        var right = await new FinancialTitleRepository(second).GetTrackedByIdAsync(payable.Id);
        var settled = left!.SettleInstallment(installmentId, account.Id, FinancialPaymentMethod.Pix, 1000m, 0m, 0m, 0m, DateTime.UtcNow, null, null, null, DateTime.UtcNow, null);
        await first.CashMovements.AddAsync(settled.Movement!);
        await first.SaveChangesAsync();

        right!.SettleInstallment(installmentId, account.Id, FinancialPaymentMethod.Pix, 1000m, 0m, 0m, 0m, DateTime.UtcNow, null, null, null, DateTime.UtcNow, null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = new InventoryDbContext(_options);
        var stored = await verify.FinancialInstallments.SingleAsync();
        Assert.Equal(0m, stored.OutstandingBalance);
        Assert.Equal(1, await verify.CashMovements.CountAsync());
        Assert.Equal(1, await verify.FinancialSettlements.CountAsync());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private static FinancialTitleEditor Editor(InventoryDbContext dbContext) =>
        new(
            new FinancialTitleRepository(dbContext),
            new UnitOfWork(dbContext),
            new FinancialPartyResolver(
                new SupplierRepository(dbContext),
                new CustomerRepository(dbContext),
                new PurchaseOrderRepository(dbContext),
                new OrderRepository(dbContext)),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System);

    private static SettleFinancialInstallmentCommand Settle(InventoryDbContext dbContext) =>
        new(
            new FinancialTitleRepository(dbContext),
            new FinancialSettlementRepository(dbContext),
            new CashAccountRepository(dbContext),
            new CashMovementRepository(dbContext),
            new UnitOfWork(dbContext),
            new NoAudit(),
            new NoUser(),
            TimeProvider.System);

    private static CreatePayableRequest ManualPayable(decimal amount) => new()
    {
        Description = "Goods",
        CounterpartyName = "Walk-up supplier",
        SourceType = FinancialSourceType.Manual,
        IssueDate = new DateOnly(2026, 11, 1),
        TotalAmount = amount,
        Installments =
        [
            new FinancialInstallmentRequest
            {
                InstallmentNumber = 1,
                DueDate = new DateOnly(2026, 12, 1),
                Amount = amount
            }
        ]
    };

    private static CreateReceivableRequest ManualReceivable(decimal amount) => new()
    {
        Description = "Sale",
        CounterpartyName = "Walk-up customer",
        SourceType = FinancialSourceType.Manual,
        IssueDate = new DateOnly(2026, 11, 1),
        TotalAmount = amount,
        Installments =
        [
            new FinancialInstallmentRequest
            {
                InstallmentNumber = 1,
                DueDate = new DateOnly(2026, 12, 1),
                Amount = amount
            }
        ]
    };

    private static SettleInstallmentRequest Settlement(Guid accountId, decimal amount) => new()
    {
        CashAccountId = accountId,
        PaymentMethod = FinancialPaymentMethod.Pix,
        SettledPrincipalAmount = amount,
        OccurredAtUtc = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc)
    };

    private sealed class ThrowingMovementRepository : ICashMovementRepository
    {
        public Task AddAsync(CashMovement movement, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Cash movement failed.");

        public Task<IReadOnlyList<CashMovement>> ListAsync(CashMovementListFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CashMovement>>([]);
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
