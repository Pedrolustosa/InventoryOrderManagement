using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Products;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Products;

public class GetAllProductsUseCaseTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task ExecuteAsync_MapsEntitiesToResponses()
    {
        var repository = Substitute.For<IProductRepository>();
        var useCase = new GetAllProductsUseCase(repository);

        var products = new List<Product>
        {
            Product.Create("A-1", "Alpha", "First", 10m, CategoryId),
            Product.Create("B-2", "Beta", null, 20m, CategoryId)
        };

        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(products);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A-1", result[0].Sku);
        Assert.Equal(CategoryId, result[0].CategoryId);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("B-2", result[1].Sku);
        Assert.Null(result[1].Description);
    }
}
