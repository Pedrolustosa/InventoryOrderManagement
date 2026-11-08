using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Application.Finance;

/// <summary>Generates unique FT-yyyyMMdd-XXXXXXXX numbers for system-created financial titles.</summary>
internal static class FinancialTitleNumbers
{
    private const int MaxAttempts = 8;

    public static async Task<string> NextAsync(
        IFinancialTitleRepository titles,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = $"FT-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (!await titles.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new FinancialConflictException("A financial title number could not be generated.");
    }
}
