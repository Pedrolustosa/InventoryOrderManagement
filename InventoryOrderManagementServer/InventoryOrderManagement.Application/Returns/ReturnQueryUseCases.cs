using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using IdentityPagedResult = InventoryOrderManagement.Application.Identity.Dtos.PagedResult<InventoryOrderManagement.Application.Identity.Dtos.SecurityAuditLogResponse>;
using ReportPagedResult = InventoryOrderManagement.Application.Reports.Dtos.PagedResult<InventoryOrderManagement.Application.Returns.Dtos.ReturnSummaryResponse>;

namespace InventoryOrderManagement.Application.Returns;

public sealed class GetAllReturnsUseCase
{
    private readonly IReturnRequestRepository _returnRepository;

    public GetAllReturnsUseCase(IReturnRequestRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReportPagedResult> ExecuteAsync(
        ReturnListQuery? query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PageRequest.Normalize(query?.Page, query?.PageSize);
        var start = NormalizeStart(query?.StartDateUtc);
        var end = NormalizeEnd(query?.EndDateUtc);

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new InvalidReturnPeriodException();
        }

        var (items, totalItems) = await _returnRepository.GetPageAsync(
            new ReturnRequestQuery
            {
                Type = query?.Type,
                Status = query?.Status,
                CustomerId = query?.CustomerId,
                SupplierId = query?.SupplierId,
                OrderId = query?.OrderId,
                PurchaseOrderId = query?.PurchaseOrderId,
                ReturnNumber = string.IsNullOrWhiteSpace(query?.ReturnNumber) ? null : query.ReturnNumber.Trim(),
                StartDateUtc = start,
                EndDateUtc = end,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return InventoryOrderManagement.Application.Reports.Dtos.PagedResult<ReturnSummaryResponse>.Create(
            items.Select(ReturnSummaryResponse.FromEntity).ToList(),
            page,
            pageSize,
            totalItems);
    }

    private static DateTime? NormalizeStart(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        return instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeEnd(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        if (instant.TimeOfDay == TimeSpan.Zero)
        {
            instant = instant.Date.AddDays(1).AddTicks(-1);
        }

        return instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }
}

public sealed class GetReturnByIdUseCase
{
    private readonly IReturnRequestRepository _returnRepository;

    public GetReturnByIdUseCase(IReturnRequestRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _returnRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ReturnNotFoundException(id);

        return ReturnResponse.FromEntity(entity);
    }
}

public sealed class GetCustomerReturnableItemsUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetCustomerReturnableItemsUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<ReturnableItemResponse>> ExecuteAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException(orderId);

        if (order.Status != OrderStatus.Delivered)
        {
            throw new ReturnConflictException("Only delivered sales orders can be returned.");
        }

        return order.Items
            .Select(item => new ReturnableItemResponse
            {
                SourceItemId = item.Id,
                ProductId = item.ProductId,
                ProductSku = item.ProductSku,
                ProductName = item.ProductName,
                SourceQuantity = item.Quantity,
                PreviouslyReturnedQuantity = item.ReturnedQuantity,
                AvailableToReturn = item.AvailableToReturn,
                UnitValueSnapshot = item.UnitPrice
            })
            .Where(item => item.AvailableToReturn > 0)
            .ToList();
    }
}

public sealed class GetSupplierReturnableItemsUseCase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public GetSupplierReturnableItemsUseCase(IPurchaseOrderRepository purchaseOrderRepository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    public async Task<IReadOnlyList<ReturnableItemResponse>> ExecuteAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

        if (order.Status is not (PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
        {
            throw new ReturnConflictException(
                "Only partially received or received purchase orders can be returned to the supplier.");
        }

        return order.Items
            .Select(item => new ReturnableItemResponse
            {
                SourceItemId = item.Id,
                ProductId = item.ProductId,
                ProductSku = item.ProductSku,
                ProductName = item.ProductName,
                SourceQuantity = item.ReceivedQuantity,
                PreviouslyReturnedQuantity = item.SupplierReturnedQuantity,
                AvailableToReturn = item.AvailableToReturnToSupplier,
                UnitValueSnapshot = item.UnitCost
            })
            .Where(item => item.AvailableToReturn > 0)
            .ToList();
    }
}

public sealed class GetReturnHistoryUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly ISecurityAuditQuery _auditQuery;

    public GetReturnHistoryUseCase(
        IReturnRequestRepository returnRepository,
        ISecurityAuditQuery auditQuery)
    {
        _returnRepository = returnRepository;
        _auditQuery = auditQuery;
    }

    public async Task<IdentityPagedResult> ExecuteAsync(
        Guid returnRequestId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        var (resolvedPage, resolvedPageSize) = PageRequest.Normalize(page, pageSize);
        return await _auditQuery.GetAsync(
            new SecurityAuditQuery
            {
                EntityType = SecurityEntities.ReturnRequest,
                EntityId = returnRequestId.ToString(),
                Page = resolvedPage,
                PageSize = resolvedPageSize
            },
            cancellationToken);
    }
}

public sealed class GetReturnStockMovementsUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetReturnStockMovementsUseCase(
        IReturnRequestRepository returnRepository,
        IInventoryRepository inventoryRepository)
    {
        _returnRepository = returnRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IReadOnlyList<ReturnStockMovementResponse>> ExecuteAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken = default)
    {
        _ = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        var movements = await _inventoryRepository.GetMovementsAsync(
            new StockMovementQuery { ReturnRequestId = returnRequestId },
            cancellationToken);

        return movements
            .Select(movement => new ReturnStockMovementResponse
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                Sku = movement.Sku,
                ProductName = movement.ProductName,
                ReturnItemId = movement.ReturnItemId,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                PreviousQuantity = movement.PreviousQuantity,
                NewQuantity = movement.NewQuantity,
                Reason = movement.Reason,
                CreatedAtUtc = movement.CreatedAtUtc
            })
            .ToList();
    }
}
