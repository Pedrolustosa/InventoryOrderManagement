using System.Reflection;
using InventoryOrderManagement.Application.Abstractions.Events;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Abstractions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Infrastructure.Events;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderManagement.Infrastructure.IntegrationTests.Events;

public class DomainEventDispatchIntegrationTests : IDisposable
{
    private readonly string _databasePath;
    private readonly ServiceProvider _serviceProvider;

    public DomainEventDispatchIntegrationTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"iom-events-{Guid.NewGuid():N}.db");

        var services = new ServiceCollection();
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlite($"Data Source={_databasePath}"));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventHandler<CategorySavedTestEvent>, CategorySavedTestHandler>();
        services.AddSingleton<HandlerProbe>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task ExecuteAsync_WithRaisedDomainEvent_InvokesHandlerOnceAfterCommit()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var probe = scope.ServiceProvider.GetRequiredService<HandlerProbe>();

        Guid categoryId = Guid.Empty;

        await unitOfWork.ExecuteAsync(async cancellationToken =>
        {
            var category = Category.Create("Domain Event Category", "integration");
            categoryId = category.Id;
            RaiseDomainEvent(category, new CategorySavedTestEvent(category.Id, DateTime.UtcNow));
            await dbContext.Categories.AddAsync(category, cancellationToken);
        });

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(categoryId, probe.LastCategoryId);
        Assert.True(probe.CategoryExistedWhenHandled);

        var persisted = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == categoryId);
        Assert.NotNull(persisted);
        Assert.Equal("Domain Event Category", persisted.Name);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
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

    private static void RaiseDomainEvent(AggregateRoot aggregate, IDomainEvent domainEvent)
    {
        var method = typeof(AggregateRoot).GetMethod(
            "RaiseDomainEvent",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        method.Invoke(aggregate, [domainEvent]);
    }

    private sealed record CategorySavedTestEvent(Guid CategoryId, DateTime OccurredOnUtc) : IDomainEvent;

    private sealed class HandlerProbe
    {
        public int CallCount { get; set; }

        public Guid LastCategoryId { get; set; }

        public bool CategoryExistedWhenHandled { get; set; }
    }

    private sealed class CategorySavedTestHandler : IDomainEventHandler<CategorySavedTestEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly HandlerProbe _probe;

        public CategorySavedTestHandler(InventoryDbContext dbContext, HandlerProbe probe)
        {
            _dbContext = dbContext;
            _probe = probe;
        }

        public async Task HandleAsync(
            CategorySavedTestEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            _probe.CallCount++;
            _probe.LastCategoryId = domainEvent.CategoryId;
            _probe.CategoryExistedWhenHandled = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Id == domainEvent.CategoryId, cancellationToken);
        }
    }
}
