using InventoryOrderManagement.Api.Security;
using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly LogoutUseCase _logoutUseCase;
    private readonly GetCurrentUserUseCase _getCurrentUserUseCase;
    private readonly ChangePasswordUseCase _changePasswordUseCase;
    private readonly IAntiforgery _antiforgery;
    private readonly IHostEnvironment _environment;

    public AuthController(
        LoginUseCase loginUseCase,
        LogoutUseCase logoutUseCase,
        GetCurrentUserUseCase getCurrentUserUseCase,
        ChangePasswordUseCase changePasswordUseCase,
        IAntiforgery antiforgery,
        IHostEnvironment environment)
    {
        _loginUseCase = loginUseCase;
        _logoutUseCase = logoutUseCase;
        _getCurrentUserUseCase = getCurrentUserUseCase;
        _changePasswordUseCase = changePasswordUseCase;
        _antiforgery = antiforgery;
        _environment = environment;
    }

    /// <summary>
    /// Issues the readable XSRF-TOKEN cookie that the Angular client sends back as X-XSRF-TOKEN.
    /// </summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Csrf()
    {
        AntiforgeryTokenCookie.Append(HttpContext, _antiforgery, _environment);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _loginUseCase.ExecuteAsync(request, cancellationToken);
        AntiforgeryTokenCookie.Append(HttpContext, _antiforgery, _environment);
        return Ok(user);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _logoutUseCase.ExecuteAsync(cancellationToken);
        AntiforgeryTokenCookie.Append(HttpContext, _antiforgery, _environment);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await _getCurrentUserUseCase.ExecuteAsync(cancellationToken);
        return Ok(user);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _changePasswordUseCase.ExecuteAsync(request, cancellationToken);
        AntiforgeryTokenCookie.Append(HttpContext, _antiforgery, _environment);
        return NoContent();
    }
}
