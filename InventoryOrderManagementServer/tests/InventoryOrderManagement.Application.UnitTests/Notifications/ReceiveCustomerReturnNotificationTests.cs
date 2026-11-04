using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Returns;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class ReceiveCustomerReturnNotificationTests
{
    private readonly IReturnRequestRepository _returns = Substitute.For<IReturnRequestRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISecurityAuditWriter _audit = Substitute.For<ISecurityAuditWriter>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<ReceiveCustomerReturnUseCase> _logger = Substitute.For<ILogger<ReceiveCustomerReturnUseCase>>();

    public ReceiveCustomerReturnNotificationTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task ExecuteAsync_NotifiesReturnAwaitingInspection()
    {
        var orderItemId = Guid.NewGuid();
        var item = ReturnItem.CreateForCustomer(
            Guid.NewGuid(),
            "SKU-1",
            "Widget",
            orderItemId,
            sourceQuantity: 5,
            previouslyReturnedQuantity: 0,
            requestedQuantity: 2,
            10m);

        var request = ReturnRequest.CreateCustomer(
            "RET-100",
            Guid.NewGuid(),
            "Jane Doe",
            Guid.NewGuid(),
            "ORD-1",
            ReturnReason.Defective,
            null,
            null,
            ReturnFinancialTreatment.CustomerCredit,
            null,
            Guid.NewGuid(),
            [item]);

        _returns.GetTrackedByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _returns.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            
            return request;
        });

        var useCase = new ReceiveCustomerReturnUseCase(
            _returns,
            _unitOfWork,
            _audit,
            _currentUser,
            _dispatcher,
            _logger);

        await useCase.ExecuteAsync(request.Id, new ReceiveCustomerReturnRequest
        {
            Items = [new ReceiveCustomerReturnItemRequest { ReturnItemId = item.Id, ReceivedQuantity = 2 }],
            Notes = "Box opened"
        });

        Assert.Equal(ReturnStatus.Received, request.Status);
        await _dispatcher.Received(1).NotifyReturnAwaitingInspectionAsync(
            request.Id,
            request.ReturnNumber,
            request.CustomerName!,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
