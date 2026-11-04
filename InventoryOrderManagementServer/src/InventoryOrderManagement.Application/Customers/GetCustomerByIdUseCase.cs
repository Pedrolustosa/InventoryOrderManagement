using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Customers;

public sealed class GetCustomerByIdUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CustomerNotFoundException(id);

        return CustomerResponse.FromEntity(customer);
    }
}
