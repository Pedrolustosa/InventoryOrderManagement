namespace InventoryOrderManagement.Application.Identity.Dtos;

public sealed class LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}

public sealed class ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }

    public required string NewPassword { get; init; }
}

public sealed class CreateUserRequest
{
    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }

    public required string Password { get; init; }

    public IReadOnlyList<Guid>? RoleIds { get; init; }
}

public sealed class UpdateUserRequest
{
    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }
}

public sealed class AssignUserRolesRequest
{
    public required IReadOnlyList<Guid> RoleIds { get; init; }
}

public sealed class UserListQuery
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public bool? IsActive { get; init; }

    public Guid? RoleId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class RoleAssignmentResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}

public sealed class UserResponse
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }

    public required bool IsActive { get; init; }

    public required bool MustChangePassword { get; init; }

    public required IReadOnlyList<RoleAssignmentResponse> Roles { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }
}

public sealed class CurrentUserResponse
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public required bool IsActive { get; init; }

    public required bool MustChangePassword { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed class CreateRoleRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}

public sealed class UpdateRoleRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}

public sealed class UpdateRolePermissionsRequest
{
    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed class RoleResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required bool IsSystemRole { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed class PermissionGroupResponse
{
    public required string Module { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed class SecurityAuditQuery
{
    public Guid? ActorUserId { get; init; }

    public Guid? TargetUserId { get; init; }

    public string? Action { get; init; }

    public string? EntityType { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class SecurityAuditLogResponse
{
    public required Guid Id { get; init; }

    public Guid? ActorUserId { get; init; }

    public Guid? TargetUserId { get; init; }

    public required string Action { get; init; }

    public required string EntityType { get; init; }

    public string? EntityId { get; init; }

    public required string Outcome { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public string? CorrelationId { get; init; }

    public string? Details { get; init; }
}

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalItems { get; init; }

    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
