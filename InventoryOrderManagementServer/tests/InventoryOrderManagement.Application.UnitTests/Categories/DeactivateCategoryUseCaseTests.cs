using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Categories;

public class DeactivateCategoryUseCaseTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly DeactivateCategoryUseCase _useCase;

    public DeactivateCategoryUseCaseTests()
    {
        _useCase = new DeactivateCategoryUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutActiveProducts_Deactivates()
    {
        var category = Category.Create("Electronics", null);
        _repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _repository.HasActiveProductsAsync(category.Id, Arg.Any<CancellationToken>()).Returns(false);

        await _useCase.ExecuteAsync(category.Id);

        Assert.False(category.IsActive);
        await _repository.Received(1).UpdateAsync(category, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveProducts_ThrowsConflict()
    {
        var category = Category.Create("Electronics", null);
        _repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _repository.HasActiveProductsAsync(category.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<CategoryHasActiveProductsException>(() =>
            _useCase.ExecuteAsync(category.Id));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
}
