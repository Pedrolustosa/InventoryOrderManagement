using InventoryOrderManagement.Application.Abstractions.Dashboard;
using InventoryOrderManagement.Application.Dashboard;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Dashboard;

public class GetDashboardRecentOrdersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsInvalidTake()
    {
        var query = Substitute.For<IDashboardQuery>();
        var useCase = new GetDashboardRecentOrdersUseCase(query);

        await Assert.ThrowsAsync<InvalidReportRequestException>(() => useCase.ExecuteAsync(0));
        await Assert.ThrowsAsync<InvalidReportRequestException>(() => useCase.ExecuteAsync(51));
    }

    [Fact]
    public async Task ExecuteAsync_MapsRows()
    {
        var query = Substitute.For<IDashboardQuery>();
        query.GetRecentOrdersAsync(10, Arg.Any<CancellationToken>()).Returns(
        [
            new DashboardRecentOrderRow
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "ORD-1",
                CustomerName = "Acme",
                Status = OrderStatus.Draft,
                Subtotal = 12.5m,
                CreatedAtUtc = DateTime.UtcNow,
                ConfirmedAtUtc = null
            }
        ]);

        var useCase = new GetDashboardRecentOrdersUseCase(query);
        var result = await useCase.ExecuteAsync(null);

        Assert.Single(result.Items);
        Assert.Equal("ORD-1", result.Items[0].OrderNumber);
        Assert.Equal(12.5m, result.Items[0].TotalAmount);
    }
}
