using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Customers;

public sealed class CreateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse> ExecuteAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var address = request.Address?.ToValueObject();
        var customer = Customer.Create(
            request.CustomerType,
            request.Name,
            request.TaxIdentifier,
            request.Email,
            request.Phone,
            address);

        if (customer.NormalizedTaxIdentifier is not null
            && await _customerRepository.ExistsByNormalizedTaxIdentifierAsync(
                customer.NormalizedTaxIdentifier,
                excludeId: null,
                cancellationToken))
        {
            throw new DuplicateCustomerTaxIdentifierException(customer.TaxIdentifier!);
        }

        await _customerRepository.AddAsync(customer, cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }
}
