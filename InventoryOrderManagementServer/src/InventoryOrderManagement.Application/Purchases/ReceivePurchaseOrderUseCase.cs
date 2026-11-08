using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class ReceivePurchaseOrderUseCase
{
    private const int MaxNumberAttempts = 5;

    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseReceiptRepository _purchaseReceiptRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public ReceivePurchaseOrderUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseReceiptRepository purchaseReceiptRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseReceiptRepository = purchaseReceiptRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<PurchaseReceiptResponse> ExecuteAsync(
        Guid purchaseOrderId,
        ReceivePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException("A receipt must contain at least one item.");
        }

        PurchaseReceipt? created = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _purchaseOrderRepository.GetTrackedByIdAsync(purchaseOrderId, ct)
                ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

            var supplier = await _supplierRepository.GetByIdAsync(order.SupplierId, ct)
                ?? throw new SupplierNotFoundException(order.SupplierId);

            if (!supplier.IsActive)
            {
                throw new InactiveSupplierStockEntryException(supplier.Id);
            }

            var lines = request.Items
                .Select(item => (item.PurchaseOrderItemId, item.QuantityReceived))
                .ToList();

            var receiptItems = new List<PurchaseReceiptItem>(lines.Count);
            foreach (var line in lines)
            {
                var orderItem = order.Items.SingleOrDefault(item => item.Id == line.PurchaseOrderItemId)
                    ?? throw new PurchaseOrderConflictException("The item does not belong to this purchase order.");

                var product = await _productRepository.GetByIdAsync(orderItem.ProductId, ct)
                    ?? throw new ProductNotFoundException(orderItem.ProductId);

                if (!product.IsActive)
                {
                    throw new InactiveProductStockEntryException(product.Id);
                }

                receiptItems.Add(PurchaseReceiptItem.Create(
                    orderItem.Id,
                    orderItem.ProductId,
                    orderItem.ProductSku,
                    orderItem.ProductName,
                    line.QuantityReceived,
                    orderItem.UnitCost));
            }

            order.Receive(lines);

            var receipt = PurchaseReceipt.Create(
                await NextNumberAsync(ct),
                order.Id,
                request.Notes,
                receiptItems);

            await _purchaseReceiptRepository.AddAsync(receipt, ct);

            foreach (var receiptItem in receipt.Items.OrderBy(item => item.ProductId))
            {
                var balance = await _inventoryRepository.GetTrackedByProductIdAsync(receiptItem.ProductId, ct)
                    ?? throw new InventoryBalanceNotFoundException(receiptItem.ProductId);

                var previousQuantity = balance.QuantityOnHand;
                balance.Receive(receiptItem.QuantityReceived);

                var movement = StockMovement.CreateForPurchase(
                    receiptItem.ProductId,
                    supplier.Id,
                    order.Id,
                    receipt.Id,
                    receiptItem.QuantityReceived,
                    previousQuantity,
                    balance.QuantityOnHand,
                    request.Notes);

                await _inventoryRepository.AddMovementAsync(movement, ct);
            }

            created = receipt;
        }, cancellationToken);

        var saved = created ?? throw new InvalidOperationException("Purchase receipt was not recorded.");
        await PurchaseAudit.WriteAsync(_auditWriter, _currentUser, SecurityActions.PurchaseReceiptCreated, saved.PurchaseOrderId, cancellationToken);
        return PurchaseReceiptResponse.FromReceipt(saved);
    }

    private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxNumberAttempts; attempt++)
        {
            var candidate = $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (!await _purchaseReceiptRepository.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException("Receipt number could not be generated.");
    }
}
