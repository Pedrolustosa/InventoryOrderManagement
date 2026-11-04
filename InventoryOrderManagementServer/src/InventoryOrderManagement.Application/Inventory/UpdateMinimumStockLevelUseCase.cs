using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class UpdateMinimumStockLevelUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMinimumStockLevelUseCase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        Guid productId,
        UpdateMinimumStockLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new ProductNotFoundException(productId);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(productId, ct)
                ?? throw new InventoryBalanceNotFoundException(productId);

            balance.SetMinimumStockLevel(request.MinimumStockLevel);
        }, cancellationToken);
    }
}
