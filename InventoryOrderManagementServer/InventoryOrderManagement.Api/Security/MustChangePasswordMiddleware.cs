using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Security;

/// <summary>
/// Blocks every route except the session endpoints while the signed-in user still has a temporary password.
/// </summary>
public sealed class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim(Permissions.MustChangePasswordClaimType, "true")
            && !IsAllowed(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Password change required",
                Detail = "Change the temporary password before using the application.",
                Instance = context.Request.Path
            });
            return;
        }

        await _next(context);
    }

    private static bool IsAllowed(HttpRequest request)
    {
        var path = request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (path.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsGet(request.Method))
        {
            return true;
        }

        if (path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(request.Method))
        {
            return true;
        }

        if (path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(request.Method))
        {
            return true;
        }

        if (path.Equals("/api/auth/csrf", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsGet(request.Method))
        {
            return true;
        }

        return false;
    }
}
