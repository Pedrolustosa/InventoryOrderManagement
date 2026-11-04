namespace InventoryOrderManagement.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
