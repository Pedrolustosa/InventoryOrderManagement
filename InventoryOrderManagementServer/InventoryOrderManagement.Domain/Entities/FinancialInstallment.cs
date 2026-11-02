using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.Finance;

namespace InventoryOrderManagement.Domain.Entities;

public class FinancialInstallment
{
    private FinancialInstallment()
    {
    }

    public Guid Id { get; private set; }

    public Guid FinancialTitleId { get; private set; }

    public int InstallmentNumber { get; private set; }

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public decimal SettledPrincipalAmount { get; private set; }

    public decimal OutstandingBalance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public int Version { get; private set; }

    public FinancialInstallmentStatus StatusOn(DateOnly today, bool titleCancelled) =>
        FinancialStatus.Installment(OutstandingBalance, SettledPrincipalAmount, DueDate, today, titleCancelled);

    internal static FinancialInstallment Create(
        Guid financialTitleId,
        int installmentNumber,
        DateOnly dueDate,
        decimal amount,
        DateTime createdAtUtc)
    {
        if (installmentNumber <= 0)
        {
            throw new DomainException("Installment number must be positive.");
        }

        var normalizedAmount = Money.Require(amount, "Installment amount");

        return new FinancialInstallment
        {
            Id = Guid.NewGuid(),
            FinancialTitleId = financialTitleId,
            InstallmentNumber = installmentNumber,
            DueDate = dueDate,
            Amount = normalizedAmount,
            SettledPrincipalAmount = 0,
            OutstandingBalance = normalizedAmount,
            CreatedAtUtc = createdAtUtc,
            Version = 0
        };
    }

    internal void ApplyPrincipal(decimal principal)
    {
        var normalized = Money.Require(principal, "Settled principal amount");
        if (normalized > OutstandingBalance)
        {
            throw new FinancialConflictException("The settlement exceeds the outstanding balance.");
        }

        SettledPrincipalAmount = Money.Round(SettledPrincipalAmount + normalized);
        OutstandingBalance = Money.Round(Amount - SettledPrincipalAmount);
        Version++;
    }
}
