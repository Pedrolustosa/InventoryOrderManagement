using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAllAsync(OrderQuery query, CancellationToken cancellationToken = default);

    Task<bool> ExistsByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    void StageItemReplacement(
        IReadOnlyCollection<OrderItem> removedItems,
        IReadOnlyCollection<OrderItem> currentItems);
}
