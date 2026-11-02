using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Domain.Entities;

public class CashMovement
{
    public const int DescriptionMaxLength = 200;

    private CashMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid CashAccountId { get; private set; }

    public Guid? FinancialSettlementId { get; private set; }

    public FinancialSettlement? Settlement { get; private set; }

    public CashMovementDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    internal static CashMovement Create(
        Guid cashAccountId,
        Guid financialSettlementId,
        CashMovementDirection direction,
        decimal amount,
        string description,
        DateTime occurredAtUtc,
        DateTime createdAtUtc)
    {
        if (cashAccountId == Guid.Empty)
        {
            throw new DomainException("Cash account is required.");
        }

        if (financialSettlementId == Guid.Empty)
        {
            throw new DomainException("Settlement is required.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new DomainException("Cash movement direction is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Cash movement description is required.");
        }

        var normalized = description.Trim();
        if (normalized.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Cash movement description cannot exceed {DescriptionMaxLength} characters.");
        }

        return new CashMovement
        {
            Id = Guid.NewGuid(),
            CashAccountId = cashAccountId,
            FinancialSettlementId = financialSettlementId,
            Direction = direction,
            Amount = Money.Require(amount, "Cash movement amount"),
            Description = normalized,
            OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc),
            CreatedAtUtc = createdAtUtc
        };
    }

    internal void Attach(FinancialSettlement settlement)
    {
        Settlement = settlement;
        FinancialSettlementId = settlement.Id;
    }
}
