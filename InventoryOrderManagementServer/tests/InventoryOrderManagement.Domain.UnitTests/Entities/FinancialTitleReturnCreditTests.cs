using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class FinancialTitleReturnCreditTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ReturnId = Guid.NewGuid();

    [Fact]
    public void ApplyReturnCredit_Partial_SettlesOldestInstallmentFirstWithoutCash()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m), (2, new DateOnly(2027, 1, 1), 100m));

        var result = title.ApplyReturnCredit(130m, ReturnId, "RET-1", Now);

        Assert.Equal(130m, result.AppliedAmount);
        Assert.False(result.Cancelled);
        Assert.Equal(2, result.Settlements.Count);
        Assert.All(result.Settlements, settlement =>
        {
            Assert.Equal(FinancialPaymentMethod.ReturnCredit, settlement.PaymentMethod);
            Assert.Equal(0m, settlement.CashAmount);
            Assert.Null(settlement.CashAccountId);
        });
        var first = title.Installments.Single(installment => installment.InstallmentNumber == 1);
        Assert.Equal(0m, first.OutstandingBalance);
        Assert.Equal(70m, title.OutstandingBalance);
        Assert.Equal(
            FinancialTitle.ReturnCreditKey(ReturnId, first.Id),
            result.Settlements.First().IdempotencyKey);
    }

    [Fact]
    public void ApplyReturnCredit_WholeUnpaidTitle_CancelsItAndKeepsTheRecord()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m));

        var result = title.ApplyReturnCredit(100m, ReturnId, "RET-1", Now);

        Assert.True(result.Cancelled);
        Assert.Empty(result.Settlements);
        Assert.NotNull(title.CancelledAtUtc);
        Assert.Equal(FinancialTitleStatus.Cancelled, title.StatusOn(new DateOnly(2026, 11, 8)));
    }

    [Fact]
    public void ApplyReturnCredit_AbovePartiallyPaidBalance_AppliesOnlyTheOutstanding()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m));
        title.SettleInstallment(
            title.Installments.Single().Id, Guid.NewGuid(), FinancialPaymentMethod.Pix, 60m, 0m, 0m, 0m,
            Now, null, null, null, Now, null);

        var result = title.ApplyReturnCredit(100m, ReturnId, "RET-1", Now);

        Assert.Equal(40m, result.AppliedAmount);
        Assert.Equal(0m, title.OutstandingBalance);
        Assert.Equal(2, title.Settlements.Count);
        Assert.Null(title.CancelledAtUtc);
    }

    [Fact]
    public void ApplyReturnCredit_OnPaidTitle_AppliesNothing()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m));
        title.SettleInstallment(
            title.Installments.Single().Id, Guid.NewGuid(), FinancialPaymentMethod.Pix, 100m, 0m, 0m, 0m,
            Now, null, null, null, Now, null);

        var result = title.ApplyReturnCredit(50m, ReturnId, "RET-1", Now);

        Assert.Equal(0m, result.AppliedAmount);
        Assert.Single(title.Settlements);
    }

    [Fact]
    public void ApplyReturnCredit_OnCancelledTitle_Throws()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m));
        title.Cancel("Duplicate", Now);

        Assert.Throws<FinancialConflictException>(() => title.ApplyReturnCredit(10m, ReturnId, "RET-1", Now));
    }

    [Fact]
    public void ReturnCredit_CannotCarryCashOrAdjustments()
    {
        var title = Receivable((1, new DateOnly(2026, 12, 1), 100m));
        var installmentId = title.Installments.Single().Id;

        Assert.Throws<DomainException>(() => title.SettleInstallment(
            installmentId, Guid.NewGuid(), FinancialPaymentMethod.ReturnCredit, 10m, 0m, 0m, 0m,
            Now, null, null, null, Now, null));
        Assert.Throws<DomainException>(() => title.SettleInstallment(
            installmentId, null, FinancialPaymentMethod.ReturnCredit, 10m, 0m, 5m, 0m,
            Now, null, null, null, Now, null));
    }

    private static FinancialTitle Receivable(params (int Number, DateOnly DueDate, decimal Amount)[] installments) =>
        FinancialTitle.Create(
            "FT-1",
            FinancialTitleType.Receivable,
            "Sale",
            FinancialCounterpartyType.Customer,
            Guid.NewGuid(),
            "Jane",
            FinancialSourceType.SalesOrder,
            Guid.NewGuid(),
            "ORD-1",
            null,
            new DateOnly(2026, 11, 1),
            installments.Sum(installment => installment.Amount),
            null,
            null,
            installments,
            Now);
}
