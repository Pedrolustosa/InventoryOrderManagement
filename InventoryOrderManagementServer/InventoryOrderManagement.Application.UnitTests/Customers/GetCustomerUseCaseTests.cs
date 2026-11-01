using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Customers;

public class GetCustomerUseCaseTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task GetById_WhenExists_ReturnsCustomer()
    {
        var customer = Customer.Create(CustomerType.Individual, "John", null, null, null, null);
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var useCase = new GetCustomerByIdUseCase(_repository);
        var response = await useCase.ExecuteAsync(customer.Id);

        Assert.Equal(customer.Id, response.Id);
        Assert.Equal("John", response.Name);
    }

    [Fact]
    public async Task GetById_WhenMissing_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var useCase = new GetCustomerByIdUseCase(_repository);

        await Assert.ThrowsAsync<CustomerNotFoundException>(() => useCase.ExecuteAsync(id));
    }

    [Fact]
    public async Task GetAll_WithFilters_MapsResponses()
    {
        var customers = new List<Customer>
        {
            Customer.Create(CustomerType.Individual, "Alpha", null, null, null, null),
            Customer.Create(CustomerType.Company, "Beta", null, null, null, null)
        };

        _repository.GetAllAsync(true, "a", CustomerType.Individual, Arg.Any<CancellationToken>())
            .Returns(customers);

        var useCase = new GetAllCustomersUseCase(_repository);
        var response = await useCase.ExecuteAsync(true, "a", CustomerType.Individual);

        Assert.Equal(2, response.Count);
        Assert.Equal("Alpha", response[0].Name);
    }
}
