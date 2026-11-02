using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Inventory;

public sealed class RegisterStockExitUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<RegisterStockExitUseCase> _logger;

    public RegisterStockExitUseCase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        INotificationDispatcher notificationDispatcher,
        ILogger<RegisterStockExitUseCase> logger)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<StockMovementResponse> ExecuteAsync(
        RegisterStockExitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(request.ProductId);

        StockMovementResponse? response = null;
        LowStockChange? lowStockChange = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var balance = await _inventoryRepository.GetTrackedByProductIdAsync(product.Id, ct)
                ?? throw new InventoryBalanceNotFoundException(product.Id);

            var previousQuantity = balance.QuantityOnHand;
            balance.Issue(request.Quantity);

            var movement = StockMovement.Create(
                product.Id,
                supplierId: null,
                StockMovementType.Exit,
                request.Quantity,
                previousQuantity,
                balance.QuantityOnHand,
                request.Reason);

            await _inventoryRepository.AddMovementAsync(movement, ct);
            response = ReceiveStockUseCase.ToResponse(movement, product.Sku, product.Name, supplierName: null);
            lowStockChange = new LowStockChange(
                product.Id,
                product.Name,
                product.Sku,
                previousQuantity,
                balance.QuantityOnHand,
                balance.MinimumStockLevel,
                movement.Id);
        }, cancellationToken);

        if (lowStockChange is not null)
        {
            await NotificationDispatch.TryNotifyLowStockAsync(
                _notificationDispatcher,
                _logger,
                [lowStockChange],
                cancellationToken);
        }

        return response ?? throw new InvalidOperationException("Stock exit was not recorded.");
    }
}
