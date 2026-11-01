using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Inventory.Dtos;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class GetAllInventoryBalancesUseCase
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetAllInventoryBalancesUseCase(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IReadOnlyList<InventoryBalanceResponse>> ExecuteAsync(
        Guid? productId,
        bool? isLowStock,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var balances = await _inventoryRepository.GetBalancesAsync(
            productId,
            isLowStock,
            isActive,
            cancellationToken);

        return balances.Select(InventoryBalanceResponse.FromDetails).ToList();
    }
}
