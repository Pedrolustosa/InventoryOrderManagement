using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class GetInventoryBalanceByProductUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryBalanceByProductUseCase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryBalanceResponse> ExecuteAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        _ = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new ProductNotFoundException(productId);

        var details = await _inventoryRepository.GetDetailsByProductIdAsync(productId, cancellationToken)
            ?? throw new InventoryBalanceNotFoundException(productId);

        return InventoryBalanceResponse.FromDetails(details);
    }
}
