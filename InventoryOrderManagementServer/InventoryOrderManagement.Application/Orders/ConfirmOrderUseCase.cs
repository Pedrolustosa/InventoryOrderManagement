using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Orders;

public sealed class ConfirmOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Order? confirmed = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var order = await _orderRepository.GetTrackedByIdAsync(orderId, ct)
                ?? throw new OrderNotFoundException(orderId);

            var customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct)
                ?? throw new CustomerNotFoundException(order.CustomerId);

            if (!customer.IsActive)
            {
                throw new InactiveCustomerOrderException(customer.Id);
            }

            var lines = order.Items.OrderBy(item => item.ProductId).ToList();
            var balances = new List<(OrderItem Item, InventoryBalance Balance)>(lines.Count);

            foreach (var item in lines)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, ct)
                    ?? throw new ProductNotFoundException(item.ProductId);

                if (!product.IsActive)
                {
                    throw new InactiveProductOrderException(product.Name);
                }

                var balance = await _inventoryRepository.GetTrackedByProductIdAsync(item.ProductId, ct);
                var available = balance?.QuantityOnHand ?? 0;

                if (balance is null || item.Quantity > available)
                {
                    throw new OrderInsufficientStockException(item.ProductName, available, item.Quantity);
                }

                balances.Add((item, balance));
            }

            foreach (var (item, balance) in balances)
            {
                var previousQuantity = balance.QuantityOnHand;
                balance.Issue(item.Quantity);

                var movement = StockMovement.CreateForOrder(
                    item.ProductId,
                    order.Id,
                    StockMovementType.OrderExit,
                    item.Quantity,
                    previousQuantity,
                    balance.QuantityOnHand,
                    $"Stock issued for order {order.OrderNumber}.");

                await _inventoryRepository.AddMovementAsync(movement, ct);
            }

            order.Confirm();
            confirmed = order;
        }, cancellationToken);

        return OrderResponse.FromOrder(confirmed ?? throw new InvalidOperationException("Order was not confirmed."));
    }
}
