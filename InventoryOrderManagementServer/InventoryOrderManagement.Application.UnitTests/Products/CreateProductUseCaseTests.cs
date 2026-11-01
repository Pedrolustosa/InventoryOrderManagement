using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Products;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Products;

public class CreateProductUseCaseTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly CreateProductUseCase _useCase;

    public CreateProductUseCaseTests()
    {
        _useCase = new CreateProductUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsAndReturnsResponse()
    {
        var request = new CreateProductRequest
        {
            Sku = "kb-001",
            Name = "Keyboard",
            Description = "RGB",
            CurrentPrice = 100m
        };

        _repository.ExistsBySkuAsync("KB-001", Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _useCase.ExecuteAsync(request);

        Assert.Equal("KB-001", response.Sku);
        Assert.Equal("Keyboard", response.Name);
        Assert.Equal("RGB", response.Description);
        Assert.Equal(100m, response.CurrentPrice);
        Assert.True(response.IsActive);
        Assert.NotEqual(Guid.Empty, response.Id);

        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(product => product.Sku == "KB-001" && product.Name == "Keyboard"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuAlreadyExists_ThrowsAndDoesNotPersist()
    {
        var request = new CreateProductRequest
        {
            Sku = "KB-001",
            Name = "Keyboard",
            CurrentPrice = 100m
        };

        _repository.ExistsBySkuAsync("KB-001", Arg.Any<CancellationToken>())
            .Returns(true);

        var exception = await Assert.ThrowsAsync<DuplicateSkuException>(() =>
            _useCase.ExecuteAsync(request));

        Assert.Equal("KB-001", exception.Sku);
        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidData_DoesNotCallRepository()
    {
        var request = new CreateProductRequest
        {
            Sku = "",
            Name = "Keyboard",
            CurrentPrice = 100m
        };

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecuteAsync(request));

        await _repository.DidNotReceive()
            .ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
}
