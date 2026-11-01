using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class AdjustStockUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustStockUseCase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdjustStockResult> ExecuteAsync(
        AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(request.ProductId);

        var movementCreated = false;
        InventoryBalanceResponse? balance = null;
        StockMovementResponse? movementResponse = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var inventory = await _inventoryRepository.GetTrackedByProductIdAsync(product.Id, ct)
                ?? throw new InventoryBalanceNotFoundException(product.Id);

            var change = inventory.AdjustTo(request.NewQuantity);
            balance = InventoryBalanceResponse.From(
                product.Id,
                product.Sku,
                product.Name,
                inventory.QuantityOnHand,
                inventory.MinimumStockLevel);

            if (change is null)
            {
                return;
            }

            var movement = Domain.Entities.StockMovement.Create(
                product.Id,
                supplierId: null,
                change.MovementType,
                change.Quantity,
                change.PreviousQuantity,
                change.NewQuantity,
                request.Reason);

            await _inventoryRepository.AddMovementAsync(movement, ct);
            movementCreated = true;
            movementResponse = ReceiveStockUseCase.ToResponse(movement, product.Sku, product.Name, supplierName: null);
        }, cancellationToken);

        return new AdjustStockResult
        {
            MovementCreated = movementCreated,
            Balance = balance ?? throw new InvalidOperationException("Stock adjustment was not evaluated."),
            Movement = movementResponse
        };
    }
}
