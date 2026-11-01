using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory.Dtos;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class GetLowStockProductsUseCase
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockProductsUseCase(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IReadOnlyList<LowStockProductResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var balances = await _inventoryRepository.GetBalancesAsync(
            productId: null,
            isLowStock: true,
            isActive: null,
            cancellationToken);

        return balances.Select(LowStockProductResponse.FromDetails).ToList();
    }
}
