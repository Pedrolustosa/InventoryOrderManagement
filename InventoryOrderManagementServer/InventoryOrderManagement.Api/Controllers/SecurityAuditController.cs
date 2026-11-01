using InventoryOrderManagement.Application.Identity;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/security/audit-logs")]
[Authorize(Policy = Permissions.Security.AuditView)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class SecurityAuditController : ControllerBase
{
    private readonly GetSecurityAuditLogsUseCase _getSecurityAuditLogsUseCase;

    public SecurityAuditController(GetSecurityAuditLogsUseCase getSecurityAuditLogsUseCase)
    {
        _getSecurityAuditLogsUseCase = getSecurityAuditLogsUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SecurityAuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SecurityAuditLogResponse>>> GetAll(
        [FromQuery] Guid? actorUserId,
        [FromQuery] Guid? targetUserId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var logs = await _getSecurityAuditLogsUseCase.ExecuteAsync(new SecurityAuditQuery
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = action,
            EntityType = entityType,
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(logs);
    }
}
