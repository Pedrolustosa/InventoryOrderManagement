using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Returns;

/// <summary>
/// Creates an exchange: a customer return of delivered items plus replacement products. Replacement prices
/// are snapshotted from the current product price; stock and the price difference are settled when the
/// inspection completes the exchange.
/// </summary>
public sealed class CreateExchangeReturnUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CreateExchangeReturnUseCase(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        CreateExchangeReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException("A return must contain at least one item.");
        }

        if (request.ReplacementItems is null || request.ReplacementItems.Count == 0)
        {
            throw new DomainException("An exchange must contain at least one replacement item.");
        }

        ReturnRequest? created = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            if (order.Status != OrderStatus.Delivered)
            {
                throw new ReturnConflictException("Only delivered sales orders can be exchanged.");
            }

            var items = CreateCustomerReturnUseCase.BuildCustomerItems(order, request.Items);
            var replacements = await BuildReplacementsAsync(request.ReplacementItems, ct);

            var entity = ReturnRequest.CreateExchange(
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
                items,
                replacements);

            await _returnRepository.AddAsync(entity, ct);
            created = entity;
        }, cancellationToken);

        var entity = created ?? throw new InvalidOperationException("Exchange was not created.");
        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ExchangeReturnCreated,
            entity.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(entity);
    }

    private async Task<List<ReturnReplacementItem>> BuildReplacementsAsync(
        IReadOnlyList<CreateReplacementItemRequest> lines,
        CancellationToken cancellationToken)
    {
        var replacements = new List<ReturnReplacementItem>(lines.Count);
        foreach (var line in lines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken)
                ?? throw new ProductNotFoundException(line.ProductId);

            if (!product.IsActive)
            {
                throw new InactiveProductOrderException(product.Name);
            }

            replacements.Add(ReturnReplacementItem.Create(
                product.Id,
                product.Sku,
                product.Name,
                line.Quantity,
                product.CurrentPrice));
        }

        return replacements;
    }
}
