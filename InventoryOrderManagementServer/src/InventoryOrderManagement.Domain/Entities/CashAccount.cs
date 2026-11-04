using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Domain.Entities;

public class CashAccount
{
    public const int NameMaxLength = 150;

    private CashAccount()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public CashAccountType AccountType { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public DateOnly OpeningBalanceDate { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static CashAccount Create(
        string name,
        CashAccountType accountType,
        decimal openingBalance,
        DateOnly openingBalanceDate,
        DateTime createdAtUtc)
    {
        return new CashAccount
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            AccountType = RequireType(accountType),
            OpeningBalance = Money.Require(openingBalance, "Opening balance", allowZero: true, allowNegative: true),
            OpeningBalanceDate = openingBalanceDate,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Update(
        string name,
        CashAccountType accountType,
        decimal openingBalance,
        DateOnly openingBalanceDate,
        bool hasMovements,
        DateTime updatedAtUtc)
    {
        var normalizedBalance = Money.Require(openingBalance, "Opening balance", allowZero: true, allowNegative: true);
        if (hasMovements && (normalizedBalance != OpeningBalance || openingBalanceDate != OpeningBalanceDate))
        {
            throw new FinancialConflictException("The opening balance cannot change after cash movements exist.");
        }

        Name = NormalizeName(name);
        AccountType = RequireType(accountType);
        if (!hasMovements)
        {
            OpeningBalance = normalizedBalance;
            OpeningBalanceDate = openingBalanceDate;
        }

        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        if (!IsActive)
        {
            throw new FinancialConflictException("The cash account is already inactive.");
        }

        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static CashAccountType RequireType(CashAccountType accountType)
    {
        if (!Enum.IsDefined(accountType))
        {
            throw new DomainException("Cash account type is required.");
        }

        return accountType;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Cash account name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new DomainException($"Cash account name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }
}
