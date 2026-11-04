using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Suppliers;

public sealed class UpdateSupplierUseCase
{
    private readonly ISupplierRepository _supplierRepository;

    public UpdateSupplierUseCase(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task ExecuteAsync(
        Guid id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new SupplierNotFoundException(id);

        if (!supplier.IsActive)
        {
            throw new InactiveSupplierException(id);
        }

        var normalizedTaxIdentifier = Supplier.NormalizeTaxIdentifierForComparison(request.TaxIdentifier);

        if (normalizedTaxIdentifier is not null
            && await _supplierRepository.ExistsByNormalizedTaxIdentifierAsync(
                normalizedTaxIdentifier,
                excludeId: id,
                cancellationToken))
        {
            throw new DuplicateTaxIdentifierException(request.TaxIdentifier!.Trim());
        }

        supplier.Update(
            request.Name,
            request.TaxIdentifier,
            request.ContactName,
            request.Email,
            request.Phone,
            request.Notes);

        await _supplierRepository.UpdateAsync(supplier, cancellationToken);
    }
}
