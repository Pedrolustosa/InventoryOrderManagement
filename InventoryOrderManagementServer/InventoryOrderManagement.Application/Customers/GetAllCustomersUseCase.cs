using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Customers;

public sealed class GetAllCustomersUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public GetAllCustomersUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IReadOnlyList<CustomerResponse>> ExecuteAsync(
        bool? isActive = null,
        string? name = null,
        CustomerType? customerType = null,
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(
            isActive,
            name,
            customerType,
            cancellationToken);

        return customers
            .Select(CustomerResponse.FromEntity)
            .ToList();
    }
}
