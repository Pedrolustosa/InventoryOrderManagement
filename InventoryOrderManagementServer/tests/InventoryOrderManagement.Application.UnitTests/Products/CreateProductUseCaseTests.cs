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
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductUseCase _useCase;

    public CreateProductUseCaseTests()
    {
        _unitOfWork
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task>>();
                return operation(call.ArgAt<CancellationToken>(1));
            });

        _useCase = new CreateProductUseCase(
            _productRepository,
            _categoryRepository,
            _inventoryRepository,
            _unitOfWork);
        _categoryRepository.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create("Electronics", null));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsAndReturnsResponse()
    {
        var request = new CreateProductRequest
        {
            Sku = "kb-001",
            Name = "Keyboard",
            Description = "RGB",
            CurrentPrice = 100m,
            CategoryId = CategoryId
        };

        _productRepository.ExistsBySkuAsync("KB-001", Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _useCase.ExecuteAsync(request);

        Assert.Equal("KB-001", response.Sku);
        Assert.Equal(CategoryId, response.CategoryId);
        Assert.Equal("Keyboard", response.Name);

        await _productRepository.Received(1).AddAsync(
            Arg.Is<Product>(product => product.Sku == "KB-001" && product.CategoryId == CategoryId),
            Arg.Any<CancellationToken>());
        await _inventoryRepository.Received(1).AddAsync(
            Arg.Is<InventoryBalance>(balance => balance.QuantityOnHand == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryMissing_ThrowsAndDoesNotPersist()
    {
        var missingId = Guid.NewGuid();
        _categoryRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var request = new CreateProductRequest
        {
            Sku = "KB-001",
            Name = "Keyboard",
            CurrentPrice = 100m,
            CategoryId = missingId
        };

        await Assert.ThrowsAsync<CategoryNotFoundException>(() => _useCase.ExecuteAsync(request));
        await _productRepository.DidNotReceive()
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSkuAlreadyExists_ThrowsAndDoesNotPersist()
    {
        var request = new CreateProductRequest
        {
            Sku = "KB-001",
            Name = "Keyboard",
            CurrentPrice = 100m,
            CategoryId = CategoryId
        };

        _productRepository.ExistsBySkuAsync("KB-001", Arg.Any<CancellationToken>())
            .Returns(true);

        var exception = await Assert.ThrowsAsync<DuplicateSkuException>(() =>
            _useCase.ExecuteAsync(request));

        Assert.Equal("KB-001", exception.Sku);
        await _productRepository.DidNotReceive()
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidData_DoesNotPersist()
    {
        var request = new CreateProductRequest
        {
            Sku = "",
            Name = "Keyboard",
            CurrentPrice = 100m,
            CategoryId = CategoryId
        };

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecuteAsync(request));

        await _productRepository.DidNotReceive()
            .ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive()
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
}
