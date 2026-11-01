using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _dbContext;

    public InventoryRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InventoryBalance?> GetTrackedByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.InventoryBalances
            .FirstOrDefaultAsync(balance => balance.ProductId == productId, cancellationToken);
    }

    public async Task<InventoryBalanceDetails?> GetDetailsByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var balances = await GetBalancesAsync(productId, isLowStock: null, isActive: null, cancellationToken);
        return balances.SingleOrDefault();
    }

    public async Task<IReadOnlyList<InventoryBalanceDetails>> GetBalancesAsync(
        Guid? productId,
        bool? isLowStock,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var rows =
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on balance.ProductId equals product.Id
            select new { balance, product };

        if (productId.HasValue)
        {
            rows = rows.Where(row => row.balance.ProductId == productId.Value);
        }

        if (isActive.HasValue)
        {
            rows = rows.Where(row => row.product.IsActive == isActive.Value);
        }

        if (isLowStock == true)
        {
            rows = rows.Where(row => row.balance.QuantityOnHand <= row.balance.MinimumStockLevel);
        }
        else if (isLowStock == false)
        {
            rows = rows.Where(row => row.balance.QuantityOnHand > row.balance.MinimumStockLevel);
        }

        return await rows
            .OrderBy(row => row.product.Name)
            .Select(row => new InventoryBalanceDetails
            {
                ProductId = row.product.Id,
                Sku = row.product.Sku,
                ProductName = row.product.Name,
                ProductIsActive = row.product.IsActive,
                QuantityOnHand = row.balance.QuantityOnHand,
                MinimumStockLevel = row.balance.MinimumStockLevel
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryBalance balance, CancellationToken cancellationToken = default)
    {
        await _dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementDetails>> GetMovementsAsync(
        StockMovementQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows =
            from movement in _dbContext.StockMovements.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on movement.ProductId equals product.Id
            join supplier in _dbContext.Suppliers.AsNoTracking()
                on movement.SupplierId equals supplier.Id into supplierJoin
            from supplier in supplierJoin.DefaultIfEmpty()
            select new { movement, product, supplier };

        if (query.ProductId.HasValue)
        {
            rows = rows.Where(row => row.movement.ProductId == query.ProductId.Value);
        }

        if (query.SupplierId.HasValue)
        {
            rows = rows.Where(row => row.movement.SupplierId == query.SupplierId.Value);
        }

        if (query.MovementType.HasValue)
        {
            rows = rows.Where(row => row.movement.MovementType == query.MovementType.Value);
        }

        if (query.StartDateUtc.HasValue)
        {
            rows = rows.Where(row => row.movement.CreatedAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            rows = rows.Where(row => row.movement.CreatedAtUtc <= query.EndDateUtc.Value);
        }

        return await rows
            .OrderByDescending(row => row.movement.CreatedAtUtc)
            .ThenByDescending(row => row.movement.Id)
            .Select(row => new StockMovementDetails
            {
                Id = row.movement.Id,
                ProductId = row.product.Id,
                Sku = row.product.Sku,
                ProductName = row.product.Name,
                SupplierId = row.movement.SupplierId,
                SupplierName = row.supplier != null ? row.supplier.Name : null,
                OrderId = row.movement.OrderId,
                MovementType = row.movement.MovementType,
                Quantity = row.movement.Quantity,
                PreviousQuantity = row.movement.PreviousQuantity,
                NewQuantity = row.movement.NewQuantity,
                Reason = row.movement.Reason,
                CreatedAtUtc = row.movement.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
