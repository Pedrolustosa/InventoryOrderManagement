using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Domain.Finance;

public static class FinancialStatus
{
    public static FinancialInstallmentStatus Installment(
        decimal outstandingBalance,
        decimal settledPrincipalAmount,
        DateOnly dueDate,
        DateOnly today,
        bool titleCancelled)
    {
        if (titleCancelled && outstandingBalance > 0)
        {
            return FinancialInstallmentStatus.Cancelled;
        }

        if (outstandingBalance == 0)
        {
            return FinancialInstallmentStatus.Paid;
        }

        if (dueDate < today)
        {
            return FinancialInstallmentStatus.Overdue;
        }

        if (settledPrincipalAmount > 0)
        {
            return FinancialInstallmentStatus.PartiallyPaid;
        }

        return FinancialInstallmentStatus.Pending;
    }

    public static FinancialTitleStatus Title(
        bool cancelled,
        decimal settledPrincipal,
        decimal outstanding,
        bool anyOverdue)
    {
        if (cancelled)
        {
            return FinancialTitleStatus.Cancelled;
        }

        if (outstanding == 0)
        {
            return FinancialTitleStatus.Paid;
        }

        if (anyOverdue)
        {
            return FinancialTitleStatus.Overdue;
        }

        if (settledPrincipal > 0)
        {
            return FinancialTitleStatus.PartiallyPaid;
        }

        return FinancialTitleStatus.Pending;
    }
}
