using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Suppliers.Dtos;

namespace InventoryOrderManagement.Application.Suppliers;

public sealed class GetSupplierByIdUseCase
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdUseCase(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierResponse> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new SupplierNotFoundException(id);

        return SupplierResponse.FromEntity(supplier);
    }
}
