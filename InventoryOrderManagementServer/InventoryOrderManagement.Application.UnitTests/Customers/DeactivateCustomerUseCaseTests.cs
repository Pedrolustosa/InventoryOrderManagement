using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Customers;

public class DeactivateCustomerUseCaseTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly DeactivateCustomerUseCase _useCase;

    public DeactivateCustomerUseCaseTests()
    {
        _useCase = new DeactivateCustomerUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_DeactivatesCustomer()
    {
        var customer = Customer.Create(CustomerType.Individual, "John", null, null, null, null);
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        await _useCase.ExecuteAsync(customer.Id);

        Assert.False(customer.IsActive);
        await _repository.Received(1).UpdateAsync(customer, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        await Assert.ThrowsAsync<CustomerNotFoundException>(() => _useCase.ExecuteAsync(id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyInactive_ThrowsDomainException()
    {
        var customer = Customer.Create(CustomerType.Individual, "John", null, null, null, null);
        customer.Deactivate();
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecuteAsync(customer.Id));
    }
}
