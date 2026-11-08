using InventoryOrderManagement.Application.Dashboard;
using InventoryOrderManagement.Application.Dashboard.Dtos;
using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Permissions.Reports.View)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class DashboardController : ControllerBase
{
    private readonly GetOperationalDashboardSummaryUseCase _getSummaryUseCase;
    private readonly GetDashboardTrendsUseCase _getTrendsUseCase;
    private readonly GetDashboardRecentOrdersUseCase _getRecentOrdersUseCase;

    public DashboardController(
        GetOperationalDashboardSummaryUseCase getSummaryUseCase,
        GetDashboardTrendsUseCase getTrendsUseCase,
        GetDashboardRecentOrdersUseCase getRecentOrdersUseCase)
    {
        _getSummaryUseCase = getSummaryUseCase;
        _getTrendsUseCase = getTrendsUseCase;
        _getRecentOrdersUseCase = getRecentOrdersUseCase;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(OperationalDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationalDashboardSummaryResponse>> GetSummary(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        CancellationToken cancellationToken)
    {
        var summary = await _getSummaryUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            cancellationToken);
        return Ok(summary);
    }

    [HttpGet("trends")]
    [ProducesResponseType(typeof(DashboardTrendsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardTrendsResponse>> GetTrends(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        [FromQuery] ReportGroupBy? groupBy,
        CancellationToken cancellationToken)
    {
        var trends = await _getTrendsUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            groupBy,
            cancellationToken);
        return Ok(trends);
    }

    [HttpGet("recent-orders")]
    [ProducesResponseType(typeof(DashboardRecentOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardRecentOrdersResponse>> GetRecentOrders(
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var orders = await _getRecentOrdersUseCase.ExecuteAsync(take, cancellationToken);
        return Ok(orders);
    }
}
