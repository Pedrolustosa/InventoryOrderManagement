using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;

namespace InventoryOrderManagement.Application.Identity;

public sealed class LoginUseCase
{
    private readonly IUserAccountService _users;

    public LoginUseCase(IUserAccountService users) => _users = users;

    public Task<CurrentUserResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken) =>
        _users.LoginAsync(request, cancellationToken);
}

public sealed class LogoutUseCase
{
    private readonly IUserAccountService _users;

    public LogoutUseCase(IUserAccountService users) => _users = users;

    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        _users.LogoutAsync(cancellationToken);
}

public sealed class GetCurrentUserUseCase
{
    private readonly IUserAccountService _users;

    public GetCurrentUserUseCase(IUserAccountService users) => _users = users;

    public Task<CurrentUserResponse> ExecuteAsync(CancellationToken cancellationToken) =>
        _users.GetCurrentUserAsync(cancellationToken);
}

public sealed class ChangePasswordUseCase
{
    private readonly IUserAccountService _users;

    public ChangePasswordUseCase(IUserAccountService users) => _users = users;

    public Task ExecuteAsync(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        _users.ChangePasswordAsync(request, cancellationToken);
}

public sealed class CreateUserUseCase
{
    private readonly IUserAccountService _users;

    public CreateUserUseCase(IUserAccountService users) => _users = users;

    public Task<UserResponse> ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken) =>
        _users.CreateAsync(request, cancellationToken);
}

public sealed class GetAllUsersUseCase
{
    private readonly IUserAccountService _users;

    public GetAllUsersUseCase(IUserAccountService users) => _users = users;

    public Task<PagedResult<UserResponse>> ExecuteAsync(UserListQuery query, CancellationToken cancellationToken) =>
        _users.GetAllAsync(query, cancellationToken);
}

public sealed class GetUserByIdUseCase
{
    private readonly IUserAccountService _users;

    public GetUserByIdUseCase(IUserAccountService users) => _users = users;

    public Task<UserResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        _users.GetByIdAsync(id, cancellationToken);
}

public sealed class UpdateUserUseCase
{
    private readonly IUserAccountService _users;

    public UpdateUserUseCase(IUserAccountService users) => _users = users;

    public Task ExecuteAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        _users.UpdateAsync(id, request, cancellationToken);
}

public sealed class DeactivateUserUseCase
{
    private readonly IUserAccountService _users;

    public DeactivateUserUseCase(IUserAccountService users) => _users = users;

    public Task ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        _users.DeactivateAsync(id, cancellationToken);
}

public sealed class AssignUserRolesUseCase
{
    private readonly IUserAccountService _users;

    public AssignUserRolesUseCase(IUserAccountService users) => _users = users;

    public Task ExecuteAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken) =>
        _users.AssignRolesAsync(id, request, cancellationToken);
}

public sealed class CreateRoleUseCase
{
    private readonly IRoleAccountService _roles;

    public CreateRoleUseCase(IRoleAccountService roles) => _roles = roles;

    public Task<RoleResponse> ExecuteAsync(CreateRoleRequest request, CancellationToken cancellationToken) =>
        _roles.CreateAsync(request, cancellationToken);
}

public sealed class GetAllRolesUseCase
{
    private readonly IRoleAccountService _roles;

    public GetAllRolesUseCase(IRoleAccountService roles) => _roles = roles;

    public Task<IReadOnlyList<RoleResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        _roles.GetAllAsync(cancellationToken);
}

public sealed class GetRoleByIdUseCase
{
    private readonly IRoleAccountService _roles;

    public GetRoleByIdUseCase(IRoleAccountService roles) => _roles = roles;

    public Task<RoleResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        _roles.GetByIdAsync(id, cancellationToken);
}

public sealed class UpdateRoleUseCase
{
    private readonly IRoleAccountService _roles;

    public UpdateRoleUseCase(IRoleAccountService roles) => _roles = roles;

    public Task ExecuteAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken) =>
        _roles.UpdateAsync(id, request, cancellationToken);
}

public sealed class DeleteRoleUseCase
{
    private readonly IRoleAccountService _roles;

    public DeleteRoleUseCase(IRoleAccountService roles) => _roles = roles;

    public Task ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        _roles.DeleteAsync(id, cancellationToken);
}

public sealed class GetAllPermissionsUseCase
{
    private readonly IRoleAccountService _roles;

    public GetAllPermissionsUseCase(IRoleAccountService roles) => _roles = roles;

    public Task<IReadOnlyList<PermissionGroupResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        _roles.GetCatalogAsync(cancellationToken);
}

public sealed class GetRolePermissionsUseCase
{
    private readonly IRoleAccountService _roles;

    public GetRolePermissionsUseCase(IRoleAccountService roles) => _roles = roles;

    public Task<IReadOnlyList<string>> ExecuteAsync(Guid roleId, CancellationToken cancellationToken) =>
        _roles.GetPermissionsAsync(roleId, cancellationToken);
}

public sealed class UpdateRolePermissionsUseCase
{
    private readonly IRoleAccountService _roles;

    public UpdateRolePermissionsUseCase(IRoleAccountService roles) => _roles = roles;

    public Task ExecuteAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken) =>
        _roles.UpdatePermissionsAsync(roleId, request, cancellationToken);
}

public sealed class GetSecurityAuditLogsUseCase
{
    private readonly ISecurityAuditQuery _audit;

    public GetSecurityAuditLogsUseCase(ISecurityAuditQuery audit) => _audit = audit;

    public Task<PagedResult<SecurityAuditLogResponse>> ExecuteAsync(
        SecurityAuditQuery query,
        CancellationToken cancellationToken) =>
        _audit.GetAsync(query, cancellationToken);
}
