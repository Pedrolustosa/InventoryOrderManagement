using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Categories.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Categories;

public class CreateCategoryUseCaseTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly CreateCategoryUseCase _useCase;

    public CreateCategoryUseCaseTests()
    {
        _useCase = new CreateCategoryUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsCategory()
    {
        _repository.ExistsByNormalizedNameAsync("ELECTRONICS", null, Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _useCase.ExecuteAsync(new CreateCategoryRequest
        {
            Name = " Electronics ",
            Description = "Devices"
        });

        Assert.Equal("Electronics", response.Name);
        Assert.True(response.IsActive);
        await _repository.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameExists_ThrowsDuplicate()
    {
        _repository.ExistsByNormalizedNameAsync("ELECTRONICS", null, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateCategoryNameException>(() =>
            _useCase.ExecuteAsync(new CreateCategoryRequest { Name = "electronics" }));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
}
