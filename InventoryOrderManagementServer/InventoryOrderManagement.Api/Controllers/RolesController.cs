using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/roles")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class RolesController : ControllerBase
{
    private readonly CreateRoleUseCase _createRoleUseCase;
    private readonly GetAllRolesUseCase _getAllRolesUseCase;
    private readonly GetRoleByIdUseCase _getRoleByIdUseCase;
    private readonly UpdateRoleUseCase _updateRoleUseCase;
    private readonly DeleteRoleUseCase _deleteRoleUseCase;
    private readonly UpdateRolePermissionsUseCase _updateRolePermissionsUseCase;

    public RolesController(
        CreateRoleUseCase createRoleUseCase,
        GetAllRolesUseCase getAllRolesUseCase,
        GetRoleByIdUseCase getRoleByIdUseCase,
        UpdateRoleUseCase updateRoleUseCase,
        DeleteRoleUseCase deleteRoleUseCase,
        UpdateRolePermissionsUseCase updateRolePermissionsUseCase)
    {
        _createRoleUseCase = createRoleUseCase;
        _getAllRolesUseCase = getAllRolesUseCase;
        _getRoleByIdUseCase = getRoleByIdUseCase;
        _updateRoleUseCase = updateRoleUseCase;
        _deleteRoleUseCase = deleteRoleUseCase;
        _updateRolePermissionsUseCase = updateRolePermissionsUseCase;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _createRoleUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Roles.View)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _getAllRolesUseCase.ExecuteAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.View)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _getRoleByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(role);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _updateRoleUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteRoleUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = Permissions.Roles.ManagePermissions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await _updateRolePermissionsUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }
}
