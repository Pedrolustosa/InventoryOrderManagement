using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Customers;

public class CreateCustomerUseCaseTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly CreateCustomerUseCase _useCase;

    public CreateCustomerUseCaseTests()
    {
        _useCase = new CreateCustomerUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsCustomer()
    {
        _repository.ExistsByNormalizedTaxIdentifierAsync("12345678900", null, Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _useCase.ExecuteAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            Name = " John Smith ",
            TaxIdentifier = "123.456.789-00",
            Email = "john@example.com"
        });

        Assert.Equal("John Smith", response.Name);
        Assert.Equal(CustomerType.Individual, response.CustomerType);
        Assert.True(response.IsActive);
        await _repository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTaxIdentifier_PersistsCustomer()
    {
        var response = await _useCase.ExecuteAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            Name = "Local Shop"
        });

        Assert.Null(response.TaxIdentifier);
        await _repository.DidNotReceive()
            .ExistsByNormalizedTaxIdentifierAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTaxIdentifierExists_ThrowsDuplicate()
    {
        _repository.ExistsByNormalizedTaxIdentifierAsync("12345678900", null, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateCustomerTaxIdentifierException>(() =>
            _useCase.ExecuteAsync(new CreateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "Other",
                TaxIdentifier = "12345678900"
            }));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }
}
