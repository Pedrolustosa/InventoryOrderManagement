namespace InventoryOrderManagement.Application.Abstractions.Persistence;

/// <summary>
/// Runs a persistence operation inside one database transaction.
/// Use cases depend on this abstraction instead of the DbContext.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
