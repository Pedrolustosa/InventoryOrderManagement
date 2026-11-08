using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Returns;

/// <summary>
/// Rebuilds, after commit, the low-stock candidates from the on-hand movements that
/// <see cref="ReturnCompletedHandler"/> recorded for a return, so the existing low-stock notification keeps
/// working now that stock moves inside the domain-event handler.
/// </summary>
internal static class ReturnLowStockChanges
{
    private static readonly StockMovementType[] OnHandTypes =
    [
        StockMovementType.CustomerReturnEntry,
        StockMovementType.SupplierReturnExit,
        StockMovementType.ExchangeReplacementExit
    ];

    public static async Task<IReadOnlyList<LowStockChange>> CollectAsync(
        IInventoryRepository inventoryRepository,
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        var movements = await inventoryRepository.GetMovementsAsync(
            new StockMovementQuery { ReturnRequestId = returnRequestId },
            cancellationToken);

        var changes = new List<LowStockChange>();
        foreach (var movement in movements.Where(movement => OnHandTypes.Contains(movement.MovementType)))
        {
            var balance = await inventoryRepository.GetDetailsByProductIdAsync(movement.ProductId, cancellationToken);
            if (balance is null)
            {
                continue;
            }

            changes.Add(new LowStockChange(
                movement.ProductId,
                movement.ProductName,
                movement.Sku,
                movement.PreviousQuantity,
                movement.NewQuantity,
                balance.MinimumStockLevel,
                movement.Id));
        }

        return changes;
    }
}
