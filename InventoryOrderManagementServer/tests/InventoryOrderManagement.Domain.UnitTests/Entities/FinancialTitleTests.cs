using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class FinancialTitleTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public void CreatePayable_TwoInstallments_IsPendingAndMovesNoCash()
    {
        var title = Payable(1500m, (1, new DateOnly(2026, 11, 15), 750m), (2, new DateOnly(2026, 12, 15), 750m));

        Assert.Equal(1500m, title.Installments.Sum(installment => installment.Amount));
        Assert.Equal(FinancialTitleStatus.Pending, title.StatusOn(new DateOnly(2026, 11, 1)));
        Assert.Empty(title.Settlements);
        Assert.Equal(1500m, title.OutstandingBalance);
    }

    [Fact]
    public void Settle_PartialPayment_LeavesOutstandingAndCreatesOutflow()
    {
        var title = Payable(1000m, (1, Today, 1000m));
        var accountId = Guid.NewGuid();

        var result = title.SettleInstallment(
            title.Installments.Single().Id,
            accountId,
            FinancialPaymentMethod.Pix,
            400m,
            0m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null);

        var installment = title.Installments.Single();
        Assert.Equal(600m, installment.OutstandingBalance);
        Assert.Equal(FinancialInstallmentStatus.PartiallyPaid, installment.StatusOn(Today, false));
        Assert.Equal(CashMovementDirection.Outflow, result.Movement!.Direction);
        Assert.Equal(400m, result.Movement.Amount);
    }

    [Fact]
    public void Settle_WithDiscountAndInterest_MovesNetCashAndMarksPaid()
    {
        var title = Payable(1000m, (1, Today, 1000m));

        var result = title.SettleInstallment(
            title.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.BankTransfer,
            1000m,
            10m,
            5m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null);

        Assert.Equal(995m, result.Settlement.CashAmount);
        Assert.Equal(0m, title.OutstandingBalance);
        Assert.Equal(FinancialTitleStatus.Paid, title.StatusOn(Today));
        Assert.Equal(995m, result.Movement!.Amount);
    }

    [Fact]
    public void CreateReceivable_DoesNotCreateASettlement()
    {
        var title = FinancialTitle.Create(
            "FT-20261101-RECEIVE1",
            FinancialTitleType.Receivable,
            "Sale",
            FinancialCounterpartyType.Customer,
            Guid.NewGuid(),
            "John Smith",
            FinancialSourceType.Manual,
            null,
            null,
            null,
            new DateOnly(2026, 11, 1),
            2500m,
            null,
            null,
            [(1, new DateOnly(2026, 11, 15), 1250m), (2, new DateOnly(2026, 12, 15), 1250m)],
            Now);

        Assert.Equal(FinancialTitleStatus.Pending, title.StatusOn(new DateOnly(2026, 11, 1)));
        Assert.Empty(title.Settlements);
    }

    [Fact]
    public void Settle_PartialReceipt_CreatesInflow()
    {
        var title = FinancialTitle.Create(
            "FT-20261101-RECEIVE2",
            FinancialTitleType.Receivable,
            "Sale",
            FinancialCounterpartyType.Customer,
            Guid.NewGuid(),
            "John Smith",
            FinancialSourceType.Manual,
            null,
            null,
            null,
            new DateOnly(2026, 11, 1),
            1000m,
            null,
            null,
            [(1, Today, 1000m)],
            Now);

        var result = title.SettleInstallment(
            title.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Cash,
            500m,
            0m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null);

        Assert.Equal(500m, title.OutstandingBalance);
        Assert.Equal(FinancialInstallmentStatus.PartiallyPaid, title.Installments.Single().StatusOn(Today, false));
        Assert.Equal(CashMovementDirection.Inflow, result.Movement!.Direction);
        Assert.Equal(500m, result.Movement.Amount);
    }

    [Fact]
    public void Status_PastDueWithoutSettlement_IsOverdue()
    {
        var title = Payable(1000m, (1, new DateOnly(2026, 11, 1), 1000m));

        Assert.Equal(FinancialInstallmentStatus.Overdue, title.Installments.Single().StatusOn(Today, false));
        Assert.Equal(FinancialTitleStatus.Overdue, title.StatusOn(Today));
    }

    [Fact]
    public void Cancel_BeforeSettlement_BlocksLaterSettlement()
    {
        var title = Payable(1000m, (1, Today, 500m), (2, Today.AddDays(30), 500m));

        title.Cancel("Entered by mistake", Now);

        Assert.Equal(FinancialTitleStatus.Cancelled, title.StatusOn(Today));
        Assert.Equal("Entered by mistake", title.CancellationReason);
        Assert.All(title.Installments, installment =>
            Assert.Equal(FinancialInstallmentStatus.Cancelled, installment.StatusOn(Today, true)));
        Assert.Throws<FinancialConflictException>(() => title.SettleInstallment(
            title.Installments.First().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Cash,
            500m,
            0m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null));
    }

    [Fact]
    public void Cancel_AfterPartialSettlement_KeepsTheSettlementAndCreatesNoMovement()
    {
        var title = Payable(1000m, (1, Today, 1000m));
        title.SettleInstallment(
            title.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Pix,
            400m,
            0m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null);

        title.Cancel("Stop the remainder", Now);

        Assert.Single(title.Settlements);
        Assert.Equal(400m, title.SettledPrincipalAmount);
        Assert.Equal(600m, title.OutstandingBalance);
        Assert.Equal(FinancialInstallmentStatus.Cancelled, title.Installments.Single().StatusOn(Today, true));
        Assert.Throws<FinancialConflictException>(() => title.SettleInstallment(
            title.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Pix,
            600m,
            0m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null));
    }

    [Fact]
    public void Settle_FullDiscount_DoesNotCreateAMovement()
    {
        var title = Payable(100m, (1, Today, 100m));

        var result = title.SettleInstallment(
            title.Installments.Single().Id,
            Guid.NewGuid(),
            FinancialPaymentMethod.Other,
            100m,
            100m,
            0m,
            0m,
            Now,
            null,
            null,
            null,
            Now,
            null);

        Assert.Equal(0m, result.Settlement.CashAmount);
        Assert.Null(result.Movement);
        Assert.Null(result.Settlement.CashAccountId);
    }

    [Fact]
    public void Create_WhenInstallmentsDoNotMatch_Throws()
    {
        Assert.Throws<DomainException>(() => Payable(1000m, (1, Today, 400m)));
    }

    private static FinancialTitle Payable(
        decimal total,
        params (int Number, DateOnly Due, decimal Amount)[] installments) =>
        FinancialTitle.Create(
            "FT-20261101-PAYABLE1",
            FinancialTitleType.Payable,
            "Goods",
            FinancialCounterpartyType.Supplier,
            Guid.NewGuid(),
            "Neighborhood Hardware",
            FinancialSourceType.Manual,
            null,
            null,
            null,
            new DateOnly(2026, 11, 1),
            total,
            null,
            null,
            installments.Select(item => (item.Number, item.Due, item.Amount)).ToArray(),
            Now);
}
