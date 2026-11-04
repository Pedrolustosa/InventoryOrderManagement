using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Suppliers;
using InventoryOrderManagement.Application.Suppliers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class SuppliersController : ControllerBase
{
    private readonly CreateSupplierUseCase _createSupplierUseCase;
    private readonly GetAllSuppliersUseCase _getAllSuppliersUseCase;
    private readonly GetSupplierByIdUseCase _getSupplierByIdUseCase;
    private readonly UpdateSupplierUseCase _updateSupplierUseCase;
    private readonly DeactivateSupplierUseCase _deactivateSupplierUseCase;

    public SuppliersController(
        CreateSupplierUseCase createSupplierUseCase,
        GetAllSuppliersUseCase getAllSuppliersUseCase,
        GetSupplierByIdUseCase getSupplierByIdUseCase,
        UpdateSupplierUseCase updateSupplierUseCase,
        DeactivateSupplierUseCase deactivateSupplierUseCase)
    {
        _createSupplierUseCase = createSupplierUseCase;
        _getAllSuppliersUseCase = getAllSuppliersUseCase;
        _getSupplierByIdUseCase = getSupplierByIdUseCase;
        _updateSupplierUseCase = updateSupplierUseCase;
        _deactivateSupplierUseCase = deactivateSupplierUseCase;
    }

    [Authorize(Policy = Permissions.Suppliers.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierResponse>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await _createSupplierUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = supplier.Id },
            supplier);
    }

    [Authorize(Policy = Permissions.Suppliers.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetAll(
        [FromQuery] bool? isActive,
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var suppliers = await _getAllSuppliersUseCase.ExecuteAsync(isActive, name, cancellationToken);
        return Ok(suppliers);
    }

    [Authorize(Policy = Permissions.Suppliers.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var supplier = await _getSupplierByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(supplier);
    }

    [Authorize(Policy = Permissions.Suppliers.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        await _updateSupplierUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.Suppliers.Deactivate)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _deactivateSupplierUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
