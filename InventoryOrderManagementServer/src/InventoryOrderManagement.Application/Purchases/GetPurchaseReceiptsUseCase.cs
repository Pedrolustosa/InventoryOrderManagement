using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class GetPurchaseReceiptsUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseReceiptRepository _purchaseReceiptRepository;

    public GetPurchaseReceiptsUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseReceiptRepository purchaseReceiptRepository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseReceiptRepository = purchaseReceiptRepository;
    }

    public async Task<IReadOnlyList<PurchaseReceiptResponse>> ExecuteAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        _ = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        var receipts = await _purchaseReceiptRepository.GetByPurchaseOrderIdAsync(purchaseOrderId, cancellationToken);
        return receipts.Select(PurchaseReceiptResponse.FromReceipt).ToList();
    }
}

public sealed class GetPurchaseReceiptByIdUseCase
{
    private readonly IPurchaseReceiptRepository _purchaseReceiptRepository;

    public GetPurchaseReceiptByIdUseCase(IPurchaseReceiptRepository purchaseReceiptRepository)
    {
        _purchaseReceiptRepository = purchaseReceiptRepository;
    }

    public async Task<PurchaseReceiptResponse> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _purchaseReceiptRepository.GetByIdAsync(receiptId, cancellationToken)
            ?? throw new PurchaseReceiptNotFoundException(receiptId);

        return PurchaseReceiptResponse.FromReceipt(receipt);
    }
}
