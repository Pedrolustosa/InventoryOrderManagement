using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Application.Orders;

/// <summary>
/// Builds the customer snapshot, shipping address and priced lines shared by draft creation and update.
/// Prices always come from the current product record.
/// </summary>
internal sealed class OrderDraftBuilder
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    public OrderDraftBuilder(ICustomerRepository customerRepository, IProductRepository productRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public async Task<PreparedOrderDraft> PrepareAsync(
        Guid customerId,
        OrderShippingAddressRequest? shippingAddress,
        IReadOnlyList<OrderItemRequest>? items,
        CancellationToken cancellationToken)
    {
        if (items is null || items.Count == 0)
        {
            throw new DomainException("An order must contain at least one item.");
        }

        if (items.Select(item => item.ProductId).Distinct().Count() != items.Count)
        {
            throw new DomainException("The same product cannot be added more than once to an order.");
        }

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException(customerId);

        if (!customer.IsActive)
        {
            throw new InactiveCustomerOrderException(customer.Id);
        }

        var lines = new List<OrderItem>(items.Count);
        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new ProductNotFoundException(item.ProductId);

            if (!product.IsActive)
            {
                throw new InactiveProductOrderException(product.Name);
            }

            lines.Add(OrderItem.Create(
                product.Id,
                product.Sku,
                product.Name,
                item.Quantity,
                product.CurrentPrice));
        }

        return new PreparedOrderDraft(customer, ResolveAddress(customer, shippingAddress), lines);
    }

    private static OrderShippingAddress ResolveAddress(Customer customer, OrderShippingAddressRequest? request)
    {
        if (request is not null)
        {
            return OrderShippingAddress.Create(
                request.PostalCode,
                request.Street,
                request.Number,
                request.Complement,
                request.Neighborhood,
                request.City,
                request.State,
                request.Country);
        }

        if (customer.Address is null)
        {
            throw new DomainException("A shipping address is required.");
        }

        return OrderShippingAddress.FromCustomerAddress(customer.Address);
    }
}

internal sealed record PreparedOrderDraft(
    Customer Customer,
    OrderShippingAddress ShippingAddress,
    IReadOnlyList<OrderItem> Items);
