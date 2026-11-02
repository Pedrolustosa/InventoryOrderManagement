using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Returns;

internal static class ReturnNumberGenerator
{
    private const int MaxAttempts = 5;

    public static async Task<string> NextAsync(
        IReturnRequestRepository repository,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (!await repository.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException("Return number could not be generated.");
    }
}
