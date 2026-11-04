using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Suppliers;

public sealed class DeactivateSupplierUseCase
{
    private readonly ISupplierRepository _supplierRepository;

    public DeactivateSupplierUseCase(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new SupplierNotFoundException(id);

        supplier.Deactivate();
        await _supplierRepository.UpdateAsync(supplier, cancellationToken);
    }
}
