using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Domain.Entities;

namespace InventoryOrderManagement.Application.Products;

/// <summary>
/// Orchestrates product registration: applies domain rules, enforces SKU uniqueness, then persists.
/// </summary>
public sealed class CreateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public CreateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponse> ExecuteAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = Product.Create(
            request.Sku,
            request.Name,
            request.Description,
            request.CurrentPrice);

        if (await _productRepository.ExistsBySkuAsync(product.Sku, cancellationToken))
        {
            throw new DuplicateSkuException(product.Sku);
        }

        await _productRepository.AddAsync(product, cancellationToken);

        return ProductResponse.FromEntity(product);
    }
}
