using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Purchases.Dtos;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class GetPurchaseOrderByIdUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public GetPurchaseOrderByIdUseCase(IPurchaseOrderRepository purchaseOrderRepository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    public async Task<PurchaseOrderResponse> ExecuteAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        return PurchaseOrderResponse.FromOrder(order);
    }
}
