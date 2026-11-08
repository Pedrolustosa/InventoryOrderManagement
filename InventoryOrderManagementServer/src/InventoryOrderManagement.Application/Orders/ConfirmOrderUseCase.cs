using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Orders;

public sealed class ConfirmOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<ConfirmOrderUseCase> _logger;

    public ConfirmOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        INotificationDispatcher notificationDispatcher,
        ILogger<ConfirmOrderUseCase> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
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

            foreach (var item in order.Items.OrderBy(line => line.ProductId))
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, ct)
                    ?? throw new ProductNotFoundException(item.ProductId);

                if (!product.IsActive)
                {
                    throw new InactiveProductOrderException(product.Name);
                }
            }

            order.Confirm();
            confirmed = order;
        }, cancellationToken);

        var orderResult = confirmed ?? throw new InvalidOperationException("Order was not confirmed.");

        await NotificationDispatch.TryAsync(
            ct => _notificationDispatcher.NotifyOrderConfirmedAsync(
                orderResult.Id,
                orderResult.OrderNumber,
                orderResult.CustomerName,
                orderResult.ConfirmedAtUtc ?? DateTime.UtcNow,
                ct),
            _logger,
            $"Failed to dispatch order-confirmed notification for order {orderResult.Id}.",
            cancellationToken);

        return OrderResponse.FromOrder(orderResult);
    }
}
