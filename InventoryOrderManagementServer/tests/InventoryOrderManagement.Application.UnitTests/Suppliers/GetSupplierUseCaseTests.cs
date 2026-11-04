using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Suppliers;

public class GetSupplierUseCaseTests
{
    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();

    [Fact]
    public async Task GetById_WhenExists_ReturnsSupplier()
    {
        var supplier = Supplier.Create("Tech", null, null, null, null, null);
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        var useCase = new GetSupplierByIdUseCase(_repository);
        var response = await useCase.ExecuteAsync(supplier.Id);

        Assert.Equal(supplier.Id, response.Id);
        Assert.Equal("Tech", response.Name);
    }

    [Fact]
    public async Task GetById_WhenMissing_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        var useCase = new GetSupplierByIdUseCase(_repository);

        await Assert.ThrowsAsync<SupplierNotFoundException>(() => useCase.ExecuteAsync(id));
    }

    [Fact]
    public async Task GetAll_WithFilters_MapsResponses()
    {
        var suppliers = new List<Supplier>
        {
            Supplier.Create("Alpha", null, null, null, null, null),
            Supplier.Create("Beta", null, null, null, null, null)
        };

        _repository.GetAllAsync(true, "a", Arg.Any<CancellationToken>()).Returns(suppliers);

        var useCase = new GetAllSuppliersUseCase(_repository);
        var response = await useCase.ExecuteAsync(true, "a");

        Assert.Equal(2, response.Count);
        Assert.Equal("Alpha", response[0].Name);
    }
}
