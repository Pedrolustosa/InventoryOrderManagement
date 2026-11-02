using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Notifications.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Notifications;

public class NotificationUseCaseTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUserNotificationRepository _repository = Substitute.For<IUserNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public NotificationUseCaseTests()
    {
        _currentUser.UserId.Returns(CurrentUserId);
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
    }

    [Fact]
    public async Task GetUserNotifications_AppliesFiltersPagingAndOrder()
    {
        var newer = CreateNotification(CurrentUserId, createdAt: DateTime.UtcNow);
        var older = CreateNotification(CurrentUserId, createdAt: DateTime.UtcNow.AddMinutes(-5));
        _repository.GetPageAsync(Arg.Any<UserNotificationQuery>(), Arg.Any<CancellationToken>())
            .Returns((Items: (IReadOnlyList<UserNotification>)[newer, older], TotalItems: 2));

        var useCase = new GetUserNotificationsUseCase(_repository, _currentUser);
        var result = await useCase.ExecuteAsync(new NotificationListQuery
        {
            IsRead = false,
            Type = NotificationType.LowStockDetected,
            Severity = NotificationSeverity.Warning,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);

        await _repository.Received(1).GetPageAsync(
            Arg.Is<UserNotificationQuery>(query =>
                query.RecipientUserId == CurrentUserId
                && query.IsRead == false
                && query.Type == NotificationType.LowStockDetected
                && query.Severity == NotificationSeverity.Warning
                && query.Page == 1
                && query.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCountForCurrentUser()
    {
        _repository.CountUnreadAsync(CurrentUserId, Arg.Any<CancellationToken>()).Returns(7);
        var useCase = new GetUnreadNotificationCountUseCase(_repository, _currentUser);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(7, result.Count);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesOwnedNotification()
    {
        var notification = CreateNotification(CurrentUserId);
        _repository.GetByIdForRecipientAsync(notification.Id, CurrentUserId, Arg.Any<CancellationToken>())
            .Returns(notification);

        var useCase = new MarkNotificationAsReadUseCase(_repository, _currentUser, _unitOfWork);
        var response = await useCase.ExecuteAsync(notification.Id);

        Assert.True(response.IsRead);
        Assert.NotNull(response.ReadAtUtc);
    }

    [Fact]
    public async Task MarkAsRead_WhenOwnedByOtherUser_ThrowsNotFound()
    {
        _repository.GetByIdForRecipientAsync(Arg.Any<Guid>(), CurrentUserId, Arg.Any<CancellationToken>())
            .Returns((UserNotification?)null);

        var useCase = new MarkNotificationAsReadUseCase(_repository, _currentUser, _unitOfWork);

        await Assert.ThrowsAsync<NotificationNotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAllAsRead_OnlyCurrentUser()
    {
        _repository.MarkAllUnreadAsReadAsync(CurrentUserId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(4);

        var useCase = new MarkAllNotificationsAsReadUseCase(_repository, _currentUser, _unitOfWork);
        var response = await useCase.ExecuteAsync();

        Assert.Equal(4, response.UpdatedCount);
        await _repository.Received(1).MarkAllUnreadAsReadAsync(
            CurrentUserId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAllUnreadAsReadAsync(
            OtherUserId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private static UserNotification CreateNotification(Guid recipientUserId, DateTime? createdAt = null)
        => UserNotification.Create(
            recipientUserId,
            NotificationType.LowStockDetected,
            NotificationSeverity.Warning,
            "Low stock detected",
            "Stock is low.",
            $"LowStockDetected:{Guid.NewGuid()}:{recipientUserId}",
            "Product",
            Guid.NewGuid(),
            "/inventory",
            createdAt ?? DateTime.UtcNow);
}
