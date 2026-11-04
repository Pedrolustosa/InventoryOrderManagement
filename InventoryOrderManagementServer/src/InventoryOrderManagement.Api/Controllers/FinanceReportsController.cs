using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/finance")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class FinanceReportsController : ControllerBase
{
    private readonly GetFinancialSettlementsUseCase _settlements;
    private readonly GetCashMovementsUseCase _movements;
    private readonly GetFinanceDashboardUseCase _dashboard;
    private readonly GetRealizedCashFlowUseCase _realized;
    private readonly GetProjectedCashFlowUseCase _projected;
    private readonly GetFinancialAgingReportUseCase _aging;

    public FinanceReportsController(
        GetFinancialSettlementsUseCase settlements,
        GetCashMovementsUseCase movements,
        GetFinanceDashboardUseCase dashboard,
        GetRealizedCashFlowUseCase realized,
        GetProjectedCashFlowUseCase projected,
        GetFinancialAgingReportUseCase aging)
    {
        _settlements = settlements;
        _movements = movements;
        _dashboard = dashboard;
        _realized = realized;
        _projected = projected;
        _aging = aging;
    }

    [Authorize(Policy = Permissions.Finance.View)]
    [HttpGet("settlements")]
    [ProducesResponseType(typeof(PagedResult<FinancialSettlementResponse>), StatusCodes.Status200OK)]
    public Task<PagedResult<FinancialSettlementResponse>> Settlements(
        [FromQuery] FinancialTitleType? type,
        [FromQuery] Guid? cashAccountId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _settlements.ExecuteAsync(type, cashAccountId, startDateUtc, endDateUtc, page, pageSize, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashFlowView)]
    [HttpGet("cash-movements")]
    [ProducesResponseType(typeof(PagedResult<CashMovementResponse>), StatusCodes.Status200OK)]
    public Task<PagedResult<CashMovementResponse>> Movements(
        [FromQuery] Guid? cashAccountId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _movements.ExecuteAsync(cashAccountId, startDateUtc, endDateUtc, page, pageSize, cancellationToken);

    [Authorize(Policy = Permissions.Finance.ReportsView)]
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(FinanceDashboardResponse), StatusCodes.Status200OK)]
    public Task<FinanceDashboardResponse> Dashboard(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken) =>
        _dashboard.ExecuteAsync(startDateUtc, endDateUtc, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashFlowView)]
    [HttpGet("cash-flow/realized")]
    [ProducesResponseType(typeof(CashFlowResponse), StatusCodes.Status200OK)]
    public Task<CashFlowResponse> Realized(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] Guid? cashAccountId,
        [FromQuery] CashFlowGrouping grouping = CashFlowGrouping.Day,
        CancellationToken cancellationToken = default) =>
        _realized.ExecuteAsync(startDateUtc, endDateUtc, cashAccountId, grouping, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashFlowView)]
    [HttpGet("cash-flow/projected")]
    [ProducesResponseType(typeof(CashFlowResponse), StatusCodes.Status200OK)]
    public Task<CashFlowResponse> Projected(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] CashFlowGrouping grouping = CashFlowGrouping.Day,
        CancellationToken cancellationToken = default) =>
        _projected.ExecuteAsync(startDateUtc, endDateUtc, grouping, cancellationToken);

    [Authorize(Policy = Permissions.Finance.ReportsView)]
    [HttpGet("reports/aging")]
    [ProducesResponseType(typeof(PagedResult<AgingReportItemResponse>), StatusCodes.Status200OK)]
    public Task<PagedResult<AgingReportItemResponse>> Aging(
        [FromQuery] FinancialTitleType? type,
        [FromQuery] Guid? counterpartyId,
        [FromQuery] FinancialInstallmentStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _aging.ExecuteAsync(type, counterpartyId, status, page, pageSize, cancellationToken);
}
