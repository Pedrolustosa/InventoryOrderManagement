using Microsoft.AspNetCore.Antiforgery;

namespace InventoryOrderManagement.Api.Security;

internal static class AntiforgeryTokenCookie
{
    public const string CookieName = "XSRF-TOKEN";

    public static void Append(HttpContext context, IAntiforgery antiforgery, IHostEnvironment environment)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append(CookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = !environment.IsDevelopment() || context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        });
    }
}
