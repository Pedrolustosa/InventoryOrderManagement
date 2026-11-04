using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Suppliers;

public sealed class CreateSupplierUseCase
{
    private readonly ISupplierRepository _supplierRepository;

    public CreateSupplierUseCase(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierResponse> ExecuteAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = Supplier.Create(
            request.Name,
            request.TaxIdentifier,
            request.ContactName,
            request.Email,
            request.Phone,
            request.Notes);

        if (supplier.NormalizedTaxIdentifier is not null
            && await _supplierRepository.ExistsByNormalizedTaxIdentifierAsync(
                supplier.NormalizedTaxIdentifier,
                excludeId: null,
                cancellationToken))
        {
            throw new DuplicateTaxIdentifierException(supplier.TaxIdentifier!);
        }

        await _supplierRepository.AddAsync(supplier, cancellationToken);

        return SupplierResponse.FromEntity(supplier);
    }
}
