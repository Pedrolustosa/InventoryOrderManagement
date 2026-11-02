using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Purchases;

public sealed class CreatePurchaseOrderUseCase
{
    private const int MaxNumberAttempts = 5;

    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseDraftBuilder _draftBuilder;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CreatePurchaseOrderUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _draftBuilder = new PurchaseDraftBuilder(supplierRepository, productRepository);
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<PurchaseOrderResponse> ExecuteAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PurchaseOrder? created = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var draft = await _draftBuilder.PrepareAsync(request.SupplierId, request.Items, ct);
            var order = PurchaseOrder.Create(
                await NextNumberAsync(ct),
                draft.Supplier.Id,
                draft.Supplier.Name,
                request.Notes,
                draft.Items);

            await _purchaseOrderRepository.AddAsync(order, ct);
            created = order;
        }, cancellationToken);

        var order = created ?? throw new InvalidOperationException("Purchase order was not created.");
        await PurchaseAudit.WriteAsync(_auditWriter, _currentUser, SecurityActions.PurchaseOrderCreated, order.Id, cancellationToken);
        return PurchaseOrderResponse.FromOrder(order);
    }

    private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxNumberAttempts; attempt++)
        {
            var candidate = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (!await _purchaseOrderRepository.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException("Purchase order number could not be generated.");
    }
}
