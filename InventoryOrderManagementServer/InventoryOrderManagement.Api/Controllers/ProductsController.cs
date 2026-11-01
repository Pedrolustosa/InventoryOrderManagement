using InventoryOrderManagement.Application.Products;
using InventoryOrderManagement.Application.Products.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateProductUseCase _createProductUseCase;
    private readonly GetAllProductsUseCase _getAllProductsUseCase;

    public ProductsController(
        CreateProductUseCase createProductUseCase,
        GetAllProductsUseCase getAllProductsUseCase)
    {
        _createProductUseCase = createProductUseCase;
        _getAllProductsUseCase = getAllProductsUseCase;
    }

    /// <summary>
    /// Registers a new product in the catalog.
    /// </summary>
    [Authorize(Policy = Permissions.Products.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _createProductUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { id = product.Id },
            product);
    }

    /// <summary>
    /// Lists all registered products.
    /// </summary>
    [Authorize(Policy = Permissions.Products.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await _getAllProductsUseCase.ExecuteAsync(cancellationToken);
        return Ok(products);
    }
}
