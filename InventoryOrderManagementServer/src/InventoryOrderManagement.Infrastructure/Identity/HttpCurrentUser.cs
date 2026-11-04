using System.Security.Claims;
using InventoryOrderManagement.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public IReadOnlySet<string> Permissions =>
        Principal?
            .FindAll(Application.Security.Permissions.ClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>(StringComparer.Ordinal);

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
}
