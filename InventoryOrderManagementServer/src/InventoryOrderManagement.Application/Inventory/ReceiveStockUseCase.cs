using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class ReceiveStockUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveStockUseCase(
        IProductRepository productRepository,
        ISupplierRepository supplierRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockMovementResponse> ExecuteAsync(
        ReceiveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(request.ProductId);

        if (!product.IsActive)
        {
            throw new InactiveProductStockEntryException(product.Id);
        }

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new SupplierNotFoundException(request.SupplierId);

        if (!supplier.IsActive)
        {
            throw new InactiveSupplierStockEntryException(supplier.Id);
        }

        StockMovementResponse? response = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(product.Id, ct)
                ?? throw new InventoryBalanceNotFoundException(product.Id);

            var previousQuantity = balance.QuantityOnHand;
            balance.Receive(request.Quantity);

            var movement = StockMovement.Create(
                product.Id,
                supplier.Id,
                StockMovementType.Entry,
                request.Quantity,
                previousQuantity,
                balance.QuantityOnHand,
                request.Reason);

            await _inventoryRepository.AddMovementAsync(movement, ct);
            response = ToResponse(movement, product.Sku, product.Name, supplier.Name);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("Stock entry was not recorded.");
    }

    internal static StockMovementResponse ToResponse(
        StockMovement movement,
        string sku,
        string productName,
        string? supplierName)
    {
        return new StockMovementResponse
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            Sku = sku,
            ProductName = productName,
            SupplierId = movement.SupplierId,
            SupplierName = supplierName,
            OrderId = movement.OrderId,
            MovementType = movement.MovementType,
            Quantity = movement.Quantity,
            PreviousQuantity = movement.PreviousQuantity,
            NewQuantity = movement.NewQuantity,
            Reason = movement.Reason,
            CreatedAtUtc = movement.CreatedAtUtc
        };
    }
}
