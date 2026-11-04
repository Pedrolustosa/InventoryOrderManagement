using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Persistence.Repositories;

public class PurchaseReceiptRepository : IPurchaseReceiptRepository
{
    private readonly InventoryDbContext _dbContext;

    public PurchaseReceiptRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default)
    {
        await _dbContext.PurchaseReceipts.AddAsync(receipt, cancellationToken);
    }

    public Task<PurchaseReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseReceipt>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .Where(receipt => receipt.PurchaseOrderId == purchaseOrderId)
            .OrderBy(receipt => receipt.ReceivedAtUtc)
            .ThenBy(receipt => receipt.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNumberAsync(string receiptNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseReceipts
            .AsNoTracking()
            .AnyAsync(receipt => receipt.ReceiptNumber == receiptNumber, cancellationToken);
    }
}
