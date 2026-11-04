using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class UsersController : ControllerBase
{
    private readonly CreateUserUseCase _createUserUseCase;
    private readonly GetAllUsersUseCase _getAllUsersUseCase;
    private readonly GetUserByIdUseCase _getUserByIdUseCase;
    private readonly UpdateUserUseCase _updateUserUseCase;
    private readonly DeactivateUserUseCase _deactivateUserUseCase;
    private readonly AssignUserRolesUseCase _assignUserRolesUseCase;

    public UsersController(
        CreateUserUseCase createUserUseCase,
        GetAllUsersUseCase getAllUsersUseCase,
        GetUserByIdUseCase getUserByIdUseCase,
        UpdateUserUseCase updateUserUseCase,
        DeactivateUserUseCase deactivateUserUseCase,
        AssignUserRolesUseCase assignUserRolesUseCase)
    {
        _createUserUseCase = createUserUseCase;
        _getAllUsersUseCase = getAllUsersUseCase;
        _getUserByIdUseCase = getUserByIdUseCase;
        _updateUserUseCase = updateUserUseCase;
        _deactivateUserUseCase = deactivateUserUseCase;
        _assignUserRolesUseCase = assignUserRolesUseCase;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Create)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _createUserUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Users.View)]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? email,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? roleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var users = await _getAllUsersUseCase.ExecuteAsync(new UserListQuery
        {
            Name = name,
            Email = email,
            IsActive = isActive,
            RoleId = roleId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Users.View)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _getUserByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Users.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await _updateUserUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.Users.Deactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _deactivateUserUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = Permissions.Users.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRoles(
        Guid id,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _assignUserRolesUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }
}
