using InventoryOrderManagement.Application.Categories;
using InventoryOrderManagement.Application.Categories.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CreateCategoryUseCase _createCategoryUseCase;
    private readonly GetAllCategoriesUseCase _getAllCategoriesUseCase;
    private readonly GetCategoryByIdUseCase _getCategoryByIdUseCase;
    private readonly UpdateCategoryUseCase _updateCategoryUseCase;
    private readonly DeactivateCategoryUseCase _deactivateCategoryUseCase;

    public CategoriesController(
        CreateCategoryUseCase createCategoryUseCase,
        GetAllCategoriesUseCase getAllCategoriesUseCase,
        GetCategoryByIdUseCase getCategoryByIdUseCase,
        UpdateCategoryUseCase updateCategoryUseCase,
        DeactivateCategoryUseCase deactivateCategoryUseCase)
    {
        _createCategoryUseCase = createCategoryUseCase;
        _getAllCategoriesUseCase = getAllCategoriesUseCase;
        _getCategoryByIdUseCase = getCategoryByIdUseCase;
        _updateCategoryUseCase = updateCategoryUseCase;
        _deactivateCategoryUseCase = deactivateCategoryUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _createCategoryUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            category);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var categories = await _getAllCategoriesUseCase.ExecuteAsync(isActive, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _getCategoryByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(category);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await _updateCategoryUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _deactivateCategoryUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
