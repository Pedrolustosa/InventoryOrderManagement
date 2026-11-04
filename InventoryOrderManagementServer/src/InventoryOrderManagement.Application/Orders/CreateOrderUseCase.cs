using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Orders;

public sealed class CreateOrderUseCase
{
    private const int MaxOrderNumberAttempts = 5;

    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDraftBuilder _draftBuilder;

    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _draftBuilder = new OrderDraftBuilder(customerRepository, productRepository);
    }

    public async Task<OrderCreatedResponse> ExecuteAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Order? created = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var draft = await _draftBuilder.PrepareAsync(
                request.CustomerId,
                request.ShippingAddress,
                request.Items,
                ct);

            var order = Order.Create(
                await NextOrderNumberAsync(ct),
                draft.Customer.Id,
                draft.Customer.Name,
                draft.ShippingAddress,
                draft.Items);

            await _orderRepository.AddAsync(order, ct);
            created = order;
        }, cancellationToken);

        return OrderCreatedResponse.FromOrder(created ?? throw new InvalidOperationException("Order was not created."));
    }

    private async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxOrderNumberAttempts; attempt++)
        {
            var candidate = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            if (!await _orderRepository.ExistsByOrderNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException("Order number could not be generated.");
    }
}
