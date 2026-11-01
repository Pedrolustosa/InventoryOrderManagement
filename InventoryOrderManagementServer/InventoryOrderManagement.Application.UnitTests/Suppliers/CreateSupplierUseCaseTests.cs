using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Entities;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Suppliers;

public class CreateSupplierUseCaseTests
{
    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();
    private readonly CreateSupplierUseCase _useCase;

    public CreateSupplierUseCaseTests()
    {
        _useCase = new CreateSupplierUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsSupplier()
    {
        _repository.ExistsByNormalizedTaxIdentifierAsync("12345678000190", null, Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _useCase.ExecuteAsync(new CreateSupplierRequest
        {
            Name = " Tech Components ",
            TaxIdentifier = "12.345.678/0001-90",
            Email = "contact@tech.com"
        });

        Assert.Equal("Tech Components", response.Name);
        Assert.Equal("12.345.678/0001-90", response.TaxIdentifier);
        Assert.True(response.IsActive);
        await _repository.Received(1).AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTaxIdentifier_PersistsSupplier()
    {
        var response = await _useCase.ExecuteAsync(new CreateSupplierRequest
        {
            Name = "Local Shop"
        });

        Assert.Equal("Local Shop", response.Name);
        Assert.Null(response.TaxIdentifier);
        await _repository.DidNotReceive()
            .ExistsByNormalizedTaxIdentifierAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTaxIdentifierExists_ThrowsDuplicate()
    {
        _repository.ExistsByNormalizedTaxIdentifierAsync("12345678000190", null, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateTaxIdentifierException>(() =>
            _useCase.ExecuteAsync(new CreateSupplierRequest
            {
                Name = "Other",
                TaxIdentifier = "12345678000190"
            }));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
    }
}
