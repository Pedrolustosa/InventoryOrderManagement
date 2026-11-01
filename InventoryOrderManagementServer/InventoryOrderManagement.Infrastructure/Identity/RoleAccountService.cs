using System.Security.Claims;
using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class RoleAccountService : IRoleAccountService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly InventoryDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ISecurityAuditWriter _audit;

    public RoleAccountService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        InventoryDbContext dbContext,
        ICurrentUser currentUser,
        ISecurityAuditWriter audit)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeRoleName(request.Name);
        var description = NormalizeDescription(request.Description);
        if (IsReserved(name))
        {
            throw new IdentityConflictException("System role names are reserved.");
        }

        if (await _roleManager.RoleExistsAsync(name))
        {
            throw new IdentityConflictException("A role with this name already exists.");
        }

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystemRole = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        IdentityResultGuard.EnsureSucceeded(await _roleManager.CreateAsync(role));
        await WriteAsync(role.Id, SecurityActions.RoleCreated, null, cancellationToken);
        return await MapAsync(role);
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var claims = await PermissionClaimsAsync(cancellationToken);
        return roles.Select(role => Map(role, claims)).ToArray();
    }

    public async Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await FindRoleAsync(id);
        return await MapAsync(role);
    }

    public async Task UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await FindRoleAsync(id);
        var name = NormalizeRoleName(request.Name);
        var description = NormalizeDescription(request.Description);

        if (role.IsSystemRole && !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new IdentityConflictException("System roles cannot be renamed.");
        }

        if (!role.IsSystemRole && IsReserved(name))
        {
            throw new IdentityConflictException("System role names are reserved.");
        }

        var existing = await _roleManager.FindByNameAsync(name);
        if (existing is not null && existing.Id != role.Id)
        {
            throw new IdentityConflictException("A role with this name already exists.");
        }

        role.Name = name;
        role.Description = description;
        IdentityResultGuard.EnsureSucceeded(await _roleManager.UpdateAsync(role));
        await WriteAsync(role.Id, SecurityActions.RoleUpdated, null, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await FindRoleAsync(id);
        if (role.IsSystemRole)
        {
            throw new IdentityConflictException("System roles cannot be deleted.");
        }

        var assigned = await _dbContext.UserRoles.CountAsync(userRole => userRole.RoleId == role.Id, cancellationToken);
        if (assigned > 0)
        {
            throw new IdentityConflictException("Remove this role from all users before deleting it.");
        }

        IdentityResultGuard.EnsureSucceeded(await _roleManager.DeleteAsync(role));
        await WriteAsync(role.Id, SecurityActions.RoleDeleted, null, cancellationToken);
    }

    public Task<IReadOnlyList<PermissionGroupResponse>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var groups = Permissions.All
            .GroupBy(permission => permission.Split('.')[0])
            .Select(group => new PermissionGroupResponse
            {
                Module = group.Key,
                Permissions = group.ToArray()
            })
            .ToArray();

        return Task.FromResult<IReadOnlyList<PermissionGroupResponse>>(groups);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await FindRoleAsync(roleId);
        return await PermissionNamesAsync(role);
    }

    public async Task UpdatePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var role = await FindRoleAsync(roleId);
        if (string.Equals(role.Name, SystemRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            throw new IdentityConflictException("Administrator permissions cannot be changed.");
        }

        var permissions = (request.Permissions ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (permissions.Any(permission => !Permissions.IsKnown(permission)))
        {
            throw new IdentityConflictException("One or more permissions are not in the catalog.");
        }

        var current = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in current.Where(claim => claim.Type == Permissions.ClaimType))
        {
            IdentityResultGuard.EnsureSucceeded(await _roleManager.RemoveClaimAsync(role, claim));
        }

        foreach (var permission in permissions)
        {
            IdentityResultGuard.EnsureSucceeded(
                await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission)));
        }

        if (!string.IsNullOrWhiteSpace(role.Name))
        {
            var members = await _userManager.GetUsersInRoleAsync(role.Name);
            foreach (var member in members)
            {
                IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(member));
            }
        }

        await WriteAsync(
            role.Id,
            SecurityActions.PermissionsUpdated,
            $"{permissions.Length} permissions.",
            cancellationToken);
    }

    private async Task<ApplicationRole> FindRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            throw new RoleNotFoundException();
        }

        return role;
    }

    private async Task<RoleResponse> MapAsync(ApplicationRole role)
    {
        var permissions = await PermissionNamesAsync(role);
        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            Permissions = permissions
        };
    }

    private async Task<IReadOnlyList<string>> PermissionNamesAsync(ApplicationRole role)
    {
        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(claim => claim.Type == Permissions.ClaimType)
            .Select(claim => claim.Value)
            .OrderBy(permission => permission)
            .ToArray();
    }

    private async Task<ILookup<Guid, string>> PermissionClaimsAsync(CancellationToken cancellationToken)
    {
        var claims = await _dbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType == Permissions.ClaimType && claim.ClaimValue != null)
            .Select(claim => new { claim.RoleId, claim.ClaimValue })
            .ToListAsync(cancellationToken);

        return claims.ToLookup(claim => claim.RoleId, claim => claim.ClaimValue!);
    }

    private static RoleResponse Map(ApplicationRole role, ILookup<Guid, string> claims)
    {
        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            Permissions = claims[role.Id].OrderBy(permission => permission).ToArray()
        };
    }

    private Task WriteAsync(Guid roleId, string action, string? details, CancellationToken cancellationToken)
    {
        return _audit.WriteAsync(new SecurityAuditEntry
        {
            ActorUserId = _currentUser.UserId,
            TargetUserId = null,
            Action = action,
            EntityType = SecurityEntities.Role,
            EntityId = roleId.ToString(),
            Outcome = SecurityOutcomes.Success,
            Details = details
        }, cancellationToken);
    }

    private static bool IsReserved(string name) =>
        SystemRoles.All.Any(role => string.Equals(role, name, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeRoleName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > 256)
        {
            throw new IdentityValidationException("Role name is required and must be at most 256 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > ApplicationRole.DescriptionMaxLength)
        {
            throw new IdentityValidationException("Role description must be at most 300 characters.");
        }

        return trimmed;
    }
}
