using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Returns;

public sealed class UpdateReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public UpdateReturnUseCase(
        IReturnRequestRepository returnRepository,
        IOrderRepository orderRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        UpdateReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException("A return must contain at least one item.");
        }

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            EnsureCanUpdate(entity.Type);

            List<ReturnItem> items;
            if (entity.Type == ReturnType.CustomerReturn)
            {
                var order = await _orderRepository.GetByIdAsync(entity.OrderId!.Value, ct)
                    ?? throw new OrderNotFoundException(entity.OrderId!.Value);

                items = CreateCustomerReturnUseCase.BuildCustomerItems(
                    order,
                    request.Items.Select(item => new CreateCustomerReturnItemRequest
                    {
                        OrderItemId = item.OrderItemId
                            ?? throw new DomainException("Order item is required."),
                        RequestedQuantity = item.RequestedQuantity
                    }).ToList());
            }
            else
            {
                var order = await _purchaseOrderRepository.GetByIdAsync(entity.PurchaseOrderId!.Value, ct)
                    ?? throw new PurchaseOrderNotFoundException(entity.PurchaseOrderId!.Value);

                items = CreateSupplierReturnUseCase.BuildSupplierItems(
                    order,
                    request.Items.Select(item => new CreateSupplierReturnItemRequest
                    {
                        PurchaseOrderItemId = item.PurchaseOrderItemId
                            ?? throw new DomainException("Purchase order item is required."),
                        RequestedQuantity = item.RequestedQuantity
                    }).ToList());
            }

            entity.UpdateDraft(
                request.Reason,
                request.ReasonDescription,
                request.Notes,
                request.FinancialTreatment,
                request.FinancialNotes,
                items);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ReturnUpdated,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }

    private void EnsureCanUpdate(ReturnType type)
    {
        var required = type == ReturnType.CustomerReturn
            ? Permissions.Returns.CustomerCreate
            : Permissions.Returns.SupplierCreate;

        if (!_currentUser.Permissions.Contains(required))
        {
            throw new IdentityForbiddenException("You do not have permission to update this return.");
        }
    }
}
