using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;

namespace InventoryOrderManagement.Application.Orders;

public sealed class UpdateDraftOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDraftBuilder _draftBuilder;

    public UpdateDraftOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _draftBuilder = new OrderDraftBuilder(customerRepository, productRepository);
    }

    public async Task ExecuteAsync(
        Guid orderId,
        UpdateDraftOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetTrackedByIdAsync(orderId, ct)
                ?? throw new OrderNotFoundException(orderId);

            var removedItems = order.Items.ToList();
            var draft = await _draftBuilder.PrepareAsync(
                request.CustomerId,
                request.ShippingAddress,
                request.Items,
                ct);

            order.ReplaceDraft(
                draft.Customer.Id,
                draft.Customer.Name,
                draft.ShippingAddress,
                draft.Items);

            _orderRepository.StageItemReplacement(removedItems, order.Items);
        }, cancellationToken);
    }
}
