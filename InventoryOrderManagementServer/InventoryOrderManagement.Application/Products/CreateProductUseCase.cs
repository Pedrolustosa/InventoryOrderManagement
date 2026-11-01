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
    private readonly ICategoryRepository _categoryRepository;

    public CreateProductUseCase(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ProductResponse> ExecuteAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new CategoryNotFoundException(request.CategoryId);

        if (!category.IsActive)
        {
            throw new InactiveCategoryException(request.CategoryId);
        }

        var product = Product.Create(
            request.Sku,
            request.Name,
            request.Description,
            request.CurrentPrice,
            request.CategoryId);

        if (await _productRepository.ExistsBySkuAsync(product.Sku, cancellationToken))
        {
            throw new DuplicateSkuException(product.Sku);
        }

        await _productRepository.AddAsync(product, cancellationToken);

        return ProductResponse.FromEntity(product);
    }
}
