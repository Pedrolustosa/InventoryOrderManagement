using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Customers;

public class UpdateCustomerUseCaseTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly UpdateCustomerUseCase _useCase;

    public UpdateCustomerUseCaseTests()
    {
        _useCase = new UpdateCustomerUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_UpdatesCustomer()
    {
        var customer = Customer.Create(CustomerType.Individual, "Old", "111", null, null, null);
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _repository.ExistsByNormalizedTaxIdentifierAsync("222", customer.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await _useCase.ExecuteAsync(customer.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            Name = "New",
            TaxIdentifier = "222"
        });

        Assert.Equal("New", customer.Name);
        Assert.Equal(CustomerType.Company, customer.CustomerType);
        await _repository.Received(1).UpdateAsync(customer, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        await Assert.ThrowsAsync<CustomerNotFoundException>(() =>
            _useCase.ExecuteAsync(id, new UpdateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "X"
            }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInactive_Throws()
    {
        var customer = Customer.Create(CustomerType.Individual, "Old", null, null, null, null);
        customer.Deactivate();
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        await Assert.ThrowsAsync<InactiveCustomerException>(() =>
            _useCase.ExecuteAsync(customer.Id, new UpdateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "X"
            }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTaxIdentifierDuplicate_Throws()
    {
        var customer = Customer.Create(CustomerType.Individual, "Old", "111", null, null, null);
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _repository.ExistsByNormalizedTaxIdentifierAsync("999", customer.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateCustomerTaxIdentifierException>(() =>
            _useCase.ExecuteAsync(customer.Id, new UpdateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                Name = "Old",
                TaxIdentifier = "999"
            }));
    }
}
