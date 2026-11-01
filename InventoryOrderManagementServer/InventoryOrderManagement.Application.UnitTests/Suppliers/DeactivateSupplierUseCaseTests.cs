using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Suppliers;

public class DeactivateSupplierUseCaseTests
{
    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();
    private readonly DeactivateSupplierUseCase _useCase;

    public DeactivateSupplierUseCaseTests()
    {
        _useCase = new DeactivateSupplierUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_DeactivatesSupplier()
    {
        var supplier = Supplier.Create("Tech", null, null, null, null, null);
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        await _useCase.ExecuteAsync(supplier.Id);

        Assert.False(supplier.IsActive);
        await _repository.Received(1).UpdateAsync(supplier, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        await Assert.ThrowsAsync<SupplierNotFoundException>(() => _useCase.ExecuteAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyInactive_ThrowsDomainException()
    {
        var supplier = Supplier.Create("Tech", null, null, null, null, null);
        supplier.Deactivate();
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecuteAsync(supplier.Id));
    }
}
