using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class GetStockMovementsUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetStockMovementsUseCase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
    }

    public Task<IReadOnlyList<StockMovementResponse>> ExecuteAsync(
        Guid? productId,
        Guid? supplierId,
        StockMovementType? movementType,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            new StockMovementQuery
            {
                ProductId = productId,
                SupplierId = supplierId,
                MovementType = movementType,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementResponse>> ExecuteForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        _ = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new ProductNotFoundException(productId);

        return await ExecuteAsync(
            productId,
            supplierId: null,
            movementType: null,
            startDateUtc: null,
            endDateUtc: null,
            cancellationToken);
    }

    private async Task<IReadOnlyList<StockMovementResponse>> QueryAsync(
        StockMovementQuery query,
        CancellationToken cancellationToken)
    {
        if (query.StartDateUtc.HasValue
            && query.EndDateUtc.HasValue
            && query.StartDateUtc.Value > query.EndDateUtc.Value)
        {
            throw new InvalidMovementPeriodException();
        }

        var movements = await _inventoryRepository.GetMovementsAsync(query, cancellationToken);
        return movements.Select(StockMovementResponse.FromDetails).ToList();
    }
}
