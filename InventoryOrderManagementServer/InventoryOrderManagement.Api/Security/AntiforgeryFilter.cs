using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryOrderManagement.Api.Security;

/// <summary>
/// Requires the X-XSRF-TOKEN header on unsafe requests. Safe methods are ignored.
/// </summary>
public sealed class AntiforgeryFilter : IAsyncAuthorizationFilter
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method)
            || HttpMethods.IsTrace(method))
        {
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid antiforgery token",
                Detail = "A valid XSRF token is required.",
                Instance = context.HttpContext.Request.Path
            });
        }
    }
}
