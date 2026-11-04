using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Returns;

public sealed class CreateCustomerReturnUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CreateCustomerReturnUseCase(
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        CreateCustomerReturnRequest request,
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
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            if (order.Status != OrderStatus.Delivered)
            {
                throw new ReturnConflictException("Only delivered sales orders can be returned.");
            }

            var items = BuildCustomerItems(order, request.Items);
            var entity = ReturnRequest.CreateCustomer(
                await ReturnNumberGenerator.NextAsync(_returnRepository, ct),
                order.CustomerId,
                order.CustomerName,
                order.Id,
                order.OrderNumber,
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
            SecurityActions.CustomerReturnCreated,
            entity.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(entity);
    }

    internal static List<ReturnItem> BuildCustomerItems(
        Order order,
        IReadOnlyList<CreateCustomerReturnItemRequest> lines)
    {
        var items = new List<ReturnItem>(lines.Count);
        var seen = new HashSet<Guid>();

        foreach (var line in lines)
        {
            if (!seen.Add(line.OrderItemId))
            {
                throw new DomainException("The same order item cannot appear more than once on a return.");
            }

            var orderItem = order.Items.SingleOrDefault(item => item.Id == line.OrderItemId)
                ?? throw new ReturnConflictException("The item does not belong to this sales order.");

            items.Add(ReturnItem.CreateForCustomer(
                orderItem.ProductId,
                orderItem.ProductSku,
                orderItem.ProductName,
                orderItem.Id,
                orderItem.Quantity,
                orderItem.ReturnedQuantity,
                line.RequestedQuantity,
                orderItem.UnitPrice));
        }

        return items;
    }
}
