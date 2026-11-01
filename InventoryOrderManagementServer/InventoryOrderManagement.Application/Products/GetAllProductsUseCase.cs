using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Products.Dtos;

namespace InventoryOrderManagement.Application.Products;

/// <summary>
/// Returns all registered products as application DTOs.
/// </summary>
public sealed class GetAllProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        return products
            .Select(ProductResponse.FromEntity)
            .ToList();
    }
}
