using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Suppliers;

public class UpdateSupplierUseCaseTests
{
    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();
    private readonly UpdateSupplierUseCase _useCase;

    public UpdateSupplierUseCaseTests()
    {
        _useCase = new UpdateSupplierUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_UpdatesSupplier()
    {
        var supplier = Supplier.Create("Old", "111", null, null, null, null);
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _repository.ExistsByNormalizedTaxIdentifierAsync("222", supplier.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await _useCase.ExecuteAsync(supplier.Id, new UpdateSupplierRequest
        {
            Name = "New",
            TaxIdentifier = "222"
        });

        Assert.Equal("New", supplier.Name);
        await _repository.Received(1).UpdateAsync(supplier, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _useCase.ExecuteAsync(id, new UpdateSupplierRequest { Name = "X" }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInactive_Throws()
    {
        var supplier = Supplier.Create("Old", null, null, null, null, null);
        supplier.Deactivate();
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        await Assert.ThrowsAsync<InactiveSupplierException>(() =>
            _useCase.ExecuteAsync(supplier.Id, new UpdateSupplierRequest { Name = "X" }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTaxIdentifierDuplicate_Throws()
    {
        var supplier = Supplier.Create("Old", "111", null, null, null, null);
        _repository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _repository.ExistsByNormalizedTaxIdentifierAsync("999", supplier.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateTaxIdentifierException>(() =>
            _useCase.ExecuteAsync(supplier.Id, new UpdateSupplierRequest
            {
                Name = "Old",
                TaxIdentifier = "999"
            }));
    }
}
