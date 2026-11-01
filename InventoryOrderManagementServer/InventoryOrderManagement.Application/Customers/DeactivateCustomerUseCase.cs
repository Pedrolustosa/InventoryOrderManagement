using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Customers;

public sealed class DeactivateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public DeactivateCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CustomerNotFoundException(id);

        customer.Deactivate();
        await _customerRepository.UpdateAsync(customer, cancellationToken);
    }
}
