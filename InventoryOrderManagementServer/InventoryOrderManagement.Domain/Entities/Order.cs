using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Aggregate root for a sales order. Status changes only through the transition methods.
/// </summary>
public class Order
{
    public const int OrderNumberMaxLength = 32;
    public const int CancellationReasonMaxLength = 300;

    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    public string CustomerName { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public OrderShippingAddress ShippingAddress { get; private set; } = null!;

    public decimal Subtotal { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? ShippedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public int Version { get; private set; }

    public static Order Create(
        string orderNumber,
        Guid customerId,
        string customerName,
        OrderShippingAddress shippingAddress,
        IReadOnlyList<OrderItem> items)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer is required.");
        }

        ArgumentNullException.ThrowIfNull(shippingAddress);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = NormalizeOrderNumber(orderNumber),
            CustomerId = customerId,
            CustomerName = NormalizeCustomerName(customerName),
            Status = OrderStatus.Draft,
            ShippingAddress = shippingAddress,
            CreatedAtUtc = DateTime.UtcNow,
            Version = 0
        };

        order.ReplaceItems(items);
        return order;
    }

    public void ReplaceDraft(
        Guid customerId,
        string customerName,
        OrderShippingAddress shippingAddress,
        IReadOnlyList<OrderItem> items)
    {
        EnsureStatus(OrderStatus.Draft);

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer is required.");
        }

        ArgumentNullException.ThrowIfNull(shippingAddress);

        CustomerId = customerId;
        CustomerName = NormalizeCustomerName(customerName);
        ShippingAddress = shippingAddress;
        ReplaceItems(items);
        Touch();
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Draft);

        if (_items.Count == 0)
        {
            throw new DomainException("An order must contain at least one item.");
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Confirmed);
        Status = OrderStatus.Shipped;
        ShippedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Deliver()
    {
        EnsureStatus(OrderStatus.Shipped);
        Status = OrderStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Cancel(string reason)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
        {
            throw new InvalidOrderStatusException(Status);
        }

        CancellationReason = NormalizeReason(reason);
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        Touch();
    }

    private void ReplaceItems(IReadOnlyList<OrderItem> items)
    {
        if (items is null || items.Count == 0)
        {
            throw new DomainException("An order must contain at least one item.");
        }

        var seen = new HashSet<Guid>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (!seen.Add(item.ProductId))
            {
                throw new DomainException("The same product cannot be added more than once to an order.");
            }
        }

        _items.Clear();

        foreach (var item in items)
        {
            item.AssignOrder(Id);
            _items.Add(item);
        }

        Subtotal = _items.Sum(item => item.LineTotal);
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOrderStatusException(Status);
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }

    private static string NormalizeOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number is required.");
        }

        var normalized = orderNumber.Trim();

        if (normalized.Length > OrderNumberMaxLength)
        {
            throw new DomainException($"Order number cannot exceed {OrderNumberMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCustomerName(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainException("Customer name is required.");
        }

        var normalized = customerName.Trim();

        if (normalized.Length > Customer.NameMaxLength)
        {
            throw new DomainException($"Customer name cannot exceed {Customer.NameMaxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancellation reason is required.");
        }

        var normalized = reason.Trim();

        if (normalized.Length > CancellationReasonMaxLength)
        {
            throw new DomainException($"Cancellation reason cannot exceed {CancellationReasonMaxLength} characters.");
        }

        return normalized;
    }
}
