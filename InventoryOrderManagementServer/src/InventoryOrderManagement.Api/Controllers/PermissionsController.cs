using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = Permissions.Roles.View)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class PermissionsController : ControllerBase
{
    private readonly GetAllPermissionsUseCase _getAllPermissionsUseCase;
    private readonly GetRolePermissionsUseCase _getRolePermissionsUseCase;

    public PermissionsController(
        GetAllPermissionsUseCase getAllPermissionsUseCase,
        GetRolePermissionsUseCase getRolePermissionsUseCase)
    {
        _getAllPermissionsUseCase = getAllPermissionsUseCase;
        _getRolePermissionsUseCase = getRolePermissionsUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionGroupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionGroupResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var permissions = await _getAllPermissionsUseCase.ExecuteAsync(cancellationToken);
        return Ok(permissions);
    }

    [HttpGet("by-role/{roleId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetByRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var permissions = await _getRolePermissionsUseCase.ExecuteAsync(roleId, cancellationToken);
        return Ok(permissions);
    }
}
