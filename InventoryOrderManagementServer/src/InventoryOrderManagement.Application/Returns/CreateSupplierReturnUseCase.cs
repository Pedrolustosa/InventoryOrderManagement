using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Returns;

public sealed class CreateSupplierReturnUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CreateSupplierReturnUseCase(
        IPurchaseOrderRepository purchaseOrderRepository,
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        CreateSupplierReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException("A return must contain at least one item.");
        }

        ReturnRequest? created = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, ct)
                ?? throw new PurchaseOrderNotFoundException(request.PurchaseOrderId);

            if (order.Status is not (PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
            {
                throw new ReturnConflictException(
                    "Only partially received or received purchase orders can be returned to the supplier.");
            }

            var items = BuildSupplierItems(order, request.Items);
            var entity = ReturnRequest.CreateSupplier(
                await ReturnNumberGenerator.NextAsync(_returnRepository, ct),
                order.SupplierId,
                order.SupplierName,
                order.Id,
                order.PurchaseOrderNumber,
                request.Reason,
                request.ReasonDescription,
                request.Notes,
                request.FinancialTreatment,
                request.FinancialNotes,
                _currentUser.UserId,
                items);

            await _returnRepository.AddAsync(entity, ct);
            created = entity;
        }, cancellationToken);

        var entity = created ?? throw new InvalidOperationException("Return was not created.");
        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.SupplierReturnCreated,
            entity.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(entity);
    }

    internal static List<ReturnItem> BuildSupplierItems(
        PurchaseOrder order,
        IReadOnlyList<CreateSupplierReturnItemRequest> lines)
    {
        var items = new List<ReturnItem>(lines.Count);
        var seen = new HashSet<Guid>();

        foreach (var line in lines)
        {
            if (!seen.Add(line.PurchaseOrderItemId))
            {
                throw new DomainException("The same purchase order item cannot appear more than once on a return.");
            }

            var orderItem = order.Items.SingleOrDefault(item => item.Id == line.PurchaseOrderItemId)
                ?? throw new ReturnConflictException("The item does not belong to this purchase order.");

            items.Add(ReturnItem.CreateForSupplier(
                orderItem.ProductId,
                orderItem.ProductSku,
                orderItem.ProductName,
                orderItem.Id,
                orderItem.ReceivedQuantity,
                orderItem.SupplierReturnedQuantity,
                line.RequestedQuantity,
                orderItem.UnitCost));
        }

        return items;
    }
}
