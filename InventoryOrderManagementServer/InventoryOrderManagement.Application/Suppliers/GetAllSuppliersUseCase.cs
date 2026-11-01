using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Suppliers.Dtos;

namespace InventoryOrderManagement.Application.Suppliers;

public sealed class GetAllSuppliersUseCase
{
    private readonly ISupplierRepository _supplierRepository;

    public GetAllSuppliersUseCase(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IReadOnlyList<SupplierResponse>> ExecuteAsync(
        bool? isActive = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(isActive, name, cancellationToken);

        return suppliers
            .Select(SupplierResponse.FromEntity)
            .ToList();
    }
}
