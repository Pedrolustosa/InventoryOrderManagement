using InventoryOrderManagement.Application.Identity.Dtos;

namespace InventoryOrderManagement.Application.Abstractions.Identity;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    IReadOnlySet<string> Permissions { get; }
}

public interface ISecurityAuditWriter
{
    Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken);
}

public sealed class SecurityAuditEntry
{
    public Guid? ActorUserId { get; init; }

    public Guid? TargetUserId { get; init; }

    public required string Action { get; init; }

    public required string EntityType { get; init; }

    public string? EntityId { get; init; }

    public required string Outcome { get; init; }

    public string? Details { get; init; }
}

public interface IUserAccountService
{
    Task<CurrentUserResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);

    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<PagedResult<UserResponse>> GetAllAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);

    Task AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken);
}

public interface IRoleAccountService
{
    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionGroupResponse>> GetCatalogAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken);

    Task UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken);
}

public interface ISecurityAuditQuery
{
    Task<PagedResult<SecurityAuditLogResponse>> GetAsync(
        SecurityAuditQuery query,
        CancellationToken cancellationToken);
}
