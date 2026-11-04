using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Customers;

public sealed class UpdateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public UpdateCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CustomerNotFoundException(id);

        if (!customer.IsActive)
        {
            throw new InactiveCustomerException(id);
        }

        var normalizedTaxIdentifier = Customer.NormalizeTaxIdentifierForComparison(request.TaxIdentifier);

        if (normalizedTaxIdentifier is not null
            && await _customerRepository.ExistsByNormalizedTaxIdentifierAsync(
                normalizedTaxIdentifier,
                excludeId: id,
                cancellationToken))
        {
            throw new DuplicateCustomerTaxIdentifierException(request.TaxIdentifier!.Trim());
        }

        var address = request.Address?.ToValueObject();

        customer.Update(
            request.CustomerType,
            request.Name,
            request.TaxIdentifier,
            request.Email,
            request.Phone,
            address);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
    }
}
