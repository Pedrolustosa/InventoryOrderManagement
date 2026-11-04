using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Infrastructure.Identity.Entities;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class UserAccountService : IUserAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly InventoryDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ISecurityAuditWriter _audit;

    public UserAccountService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        InventoryDbContext dbContext,
        ICurrentUser currentUser,
        ISecurityAuditWriter audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<CurrentUserResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var user = email.Length == 0 ? null : await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            await WriteAsync(null, null, SecurityActions.LoginFailed, SecurityEntities.Session, null, SecurityOutcomes.Failure, "Invalid credentials.", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            await WriteAsync(null, user.Id, SecurityActions.LoginFailed, SecurityEntities.Session, user.Id.ToString(), SecurityOutcomes.Failure, "Inactive account.", cancellationToken);
            throw new InvalidCredentialsException();
        }

        var signIn = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password ?? string.Empty,
            lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            await WriteAsync(null, user.Id, SecurityActions.AccountLocked, SecurityEntities.Session, user.Id.ToString(), SecurityOutcomes.Failure, "Account locked.", cancellationToken);
            throw new AccountLockedException();
        }

        if (!signIn.Succeeded)
        {
            await WriteAsync(null, user.Id, SecurityActions.LoginFailed, SecurityEntities.Session, user.Id.ToString(), SecurityOutcomes.Failure, "Invalid credentials.", cancellationToken);
            throw new InvalidCredentialsException();
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateAsync(user));
        await _signInManager.SignInAsync(user, isPersistent: false);

        await WriteAsync(user.Id, user.Id, SecurityActions.LoginSucceeded, SecurityEntities.Session, user.Id.ToString(), SecurityOutcomes.Success, null, cancellationToken);
        return await MapCurrentAsync(user);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        await _signInManager.SignOutAsync();
        await WriteAsync(userId, userId, SecurityActions.Logout, SecurityEntities.Session, userId?.ToString(), SecurityOutcomes.Success, null, cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await RequireCurrentUserAsync();
        return await MapCurrentAsync(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireCurrentUserAsync();
        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword ?? string.Empty,
            request.NewPassword ?? string.Empty);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
            {
                throw new IdentityValidationException("The current password is incorrect.");
            }

            IdentityResultGuard.EnsureSucceeded(result);
        }

        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateAsync(user));
        await _signInManager.RefreshSignInAsync(user);

        await WriteAsync(user.Id, user.Id, SecurityActions.PasswordChanged, SecurityEntities.User, user.Id.ToString(), SecurityOutcomes.Success, null, cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var displayName = NormalizeDisplayName(request.DisplayName);
        var email = NormalizeEmail(request.Email);
        var phone = NormalizePhone(request.PhoneNumber);
        var roles = await LoadRolesAsync(request.RoleIds);
        await EnsureActorCanGrantAsync(roles);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            PhoneNumber = phone,
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        IdentityResultGuard.EnsureSucceeded(await _userManager.CreateAsync(user, request.Password ?? string.Empty));

        try
        {
            if (roles.Count > 0)
            {
                IdentityResultGuard.EnsureSucceeded(
                    await _userManager.AddToRolesAsync(user, roles.Select(role => role.Name!)));
            }
        }
        catch
        {
            await _userManager.DeleteAsync(user);
            throw;
        }

        await WriteAsync(_currentUser.UserId, user.Id, SecurityActions.UserCreated, SecurityEntities.User, user.Id.ToString(), SecurityOutcomes.Success, null, cancellationToken);
        return await MapUserAsync(user, cancellationToken);
    }

    public async Task<PagedResult<UserResponse>> GetAllAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page;
        var pageSize = query.PageSize == 0 ? 20 : query.PageSize;
        EnsurePage(page, pageSize);

        var users = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            users = users.Where(user => user.DisplayName.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim().ToLower();
            users = users.Where(user => user.Email != null && user.Email.ToLower().Contains(email));
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        }

        if (query.RoleId.HasValue)
        {
            var roleId = query.RoleId.Value;
            users = users.Where(user => _dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == roleId));
        }

        var total = await users.CountAsync(cancellationToken);
        var pageItems = await users
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserListRow(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.MustChangePassword,
                user.CreatedAtUtc,
                user.UpdatedAtUtc,
                user.LastLoginAtUtc))
            .ToListAsync(cancellationToken);

        var withRoles = await AttachRolesAsync(
            pageItems.Select(user => new UserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc,
                LastLoginAtUtc = user.LastLoginAtUtc,
                Roles = []
            }).ToArray(),
            cancellationToken);
        return new PagedResult<UserResponse>
        {
            Items = withRoles,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(id);
        return await MapUserAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(id);
        var displayName = NormalizeDisplayName(request.DisplayName);
        var email = NormalizeEmail(request.Email);
        var phone = NormalizePhone(request.PhoneNumber);

        IdentityResultGuard.EnsureSucceeded(await _userManager.SetEmailAsync(user, email));
        IdentityResultGuard.EnsureSucceeded(await _userManager.SetUserNameAsync(user, email));
        IdentityResultGuard.EnsureSucceeded(await _userManager.SetPhoneNumberAsync(user, phone));

        user.DisplayName = displayName;
        user.UpdatedAtUtc = DateTime.UtcNow;
        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateAsync(user));

        await WriteAsync(_currentUser.UserId, user.Id, SecurityActions.UserUpdated, SecurityEntities.User, user.Id.ToString(), SecurityOutcomes.Success, null, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(id);
        if (_currentUser.UserId == user.Id)
        {
            throw new IdentityConflictException("You cannot deactivate your own account.");
        }

        if (user.IsActive && await _userManager.IsInRoleAsync(user, SystemRoles.Administrator))
        {
            if (await CountActiveAdministratorsAsync(cancellationToken) <= 1)
            {
                throw new IdentityConflictException("The last active administrator cannot be deactivated.");
            }
        }

        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateAsync(user));
        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

        await WriteAsync(_currentUser.UserId, user.Id, SecurityActions.UserDeactivated, SecurityEntities.User, user.Id.ToString(), SecurityOutcomes.Success, null, cancellationToken);
    }

    public async Task AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(id);
        if (_currentUser.UserId == user.Id)
        {
            throw new IdentityConflictException("You cannot change your own roles.");
        }

        var roleIds = request.RoleIds ?? [];
        if (roleIds.Count != roleIds.Distinct().Count())
        {
            throw new IdentityConflictException("Duplicate roles are not allowed.");
        }

        var roles = await LoadRolesAsync(roleIds);
        await EnsureActorCanGrantAsync(roles);
        await EnsureLastAdministratorKeepsRoleAsync(user, roles, cancellationToken);

        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0)
        {
            IdentityResultGuard.EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, current));
        }

        if (roles.Count > 0)
        {
            IdentityResultGuard.EnsureSucceeded(
                await _userManager.AddToRolesAsync(user, roles.Select(role => role.Name!)));
        }

        IdentityResultGuard.EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

        var names = string.Join(", ", roles.Select(role => role.Name).OrderBy(name => name));
        await WriteAsync(_currentUser.UserId, user.Id, SecurityActions.RolesAssigned, SecurityEntities.User, user.Id.ToString(), SecurityOutcomes.Success, names, cancellationToken);
    }

    private async Task<ApplicationUser> RequireCurrentUserAsync()
    {
        if (_currentUser.UserId is null)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null || !user.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        return user;
    }

    private async Task<ApplicationUser> FindUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            throw new UserNotFoundException();
        }

        return user;
    }

    private async Task<IReadOnlyList<ApplicationRole>> LoadRolesAsync(IReadOnlyCollection<Guid>? roleIds)
    {
        if (roleIds is null || roleIds.Count == 0)
        {
            return [];
        }

        var roles = new List<ApplicationRole>(roleIds.Count);
        foreach (var roleId in roleIds)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
            {
                throw new RoleNotFoundException();
            }

            roles.Add(role);
        }

        return roles;
    }

    private async Task EnsureActorCanGrantAsync(IReadOnlyList<ApplicationRole> roles)
    {
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                if (claim.Type == Permissions.ClaimType && !_currentUser.Permissions.Contains(claim.Value))
                {
                    throw new IdentityForbiddenException(
                        "The assigned roles grant permissions that the current user does not have.");
                }
            }
        }
    }

    private async Task EnsureLastAdministratorKeepsRoleAsync(
        ApplicationUser user,
        IReadOnlyList<ApplicationRole> roles,
        CancellationToken cancellationToken)
    {
        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, SystemRoles.Administrator))
        {
            return;
        }

        var keepsAdministrator = roles.Any(role =>
            string.Equals(role.Name, SystemRoles.Administrator, StringComparison.OrdinalIgnoreCase));

        if (!keepsAdministrator && await CountActiveAdministratorsAsync(cancellationToken) <= 1)
        {
            throw new IdentityConflictException(
                "The last active administrator must keep the Administrator role.");
        }
    }

    private async Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken)
    {
        var administrator = await _roleManager.FindByNameAsync(SystemRoles.Administrator);
        if (administrator is null)
        {
            return 0;
        }

        return await (
            from userRole in _dbContext.UserRoles
            join user in _dbContext.Users on userRole.UserId equals user.Id
            where userRole.RoleId == administrator.Id && user.IsActive
            select user.Id).CountAsync(cancellationToken);
    }

    private async Task<UserResponse> MapUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var response = new UserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            Roles = []
        };

        var mapped = await AttachRolesAsync([response], cancellationToken);
        return mapped[0];
    }

    private async Task<IReadOnlyList<UserResponse>> AttachRolesAsync(
        IReadOnlyList<UserResponse> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0)
        {
            return users;
        }

        var ids = users.Select(user => user.Id).ToArray();
        var links = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, role.Id, role.Name })
            .ToListAsync(cancellationToken);

        return users.Select(user => new UserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            Roles = links
                .Where(link => link.UserId == user.Id)
                .Select(link => new RoleAssignmentResponse
                {
                    Id = link.Id,
                    Name = link.Name ?? string.Empty
                })
                .OrderBy(role => role.Name)
                .ToArray()
        }).ToArray();
    }

    private async Task<CurrentUserResponse> MapCurrentAsync(ApplicationUser user)
    {
        var roleNames = await _userManager.GetRolesAsync(user);
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await _roleManager.GetClaimsAsync(role))
            {
                if (claim.Type == Permissions.ClaimType)
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return new CurrentUserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            Roles = roleNames.OrderBy(name => name).ToArray(),
            Permissions = permissions.OrderBy(permission => permission).ToArray()
        };
    }

    private Task WriteAsync(
        Guid? actorUserId,
        Guid? targetUserId,
        string action,
        string entityType,
        string? entityId,
        string outcome,
        string? details,
        CancellationToken cancellationToken)
    {
        return _audit.WriteAsync(new SecurityAuditEntry
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome,
            Details = details
        }, cancellationToken);
    }

    private static void EnsurePage(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new IdentityValidationException("Page must be at least 1 and page size must be between 1 and 100.");
        }
    }

    private static string NormalizeDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > ApplicationUser.DisplayNameMaxLength)
        {
            throw new IdentityValidationException("Display name is required and must be at most 150 characters.");
        }

        return trimmed;
    }

    private static string NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length == 0
            || trimmed.Length > 256
            || !trimmed.Contains('@')
            || trimmed.Contains(' '))
        {
            throw new IdentityValidationException("A valid email address is required.");
        }

        return trimmed;
    }

    private sealed record UserListRow(
        Guid Id,
        string DisplayName,
        string Email,
        string? PhoneNumber,
        bool IsActive,
        bool MustChangePassword,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        DateTime? LastLoginAtUtc);

    private static string? NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        if (trimmed.Length > ApplicationUser.PhoneNumberMaxLength)
        {
            throw new IdentityValidationException("Phone number must be at most 30 characters.");
        }

        return trimmed;
    }
}
