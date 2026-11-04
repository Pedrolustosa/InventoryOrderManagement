using InventoryOrderManagement.Application.Reports;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Permissions.Reports.View)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ReportsController : ControllerBase
{
    private readonly GetDashboardSummaryUseCase _getDashboardSummaryUseCase;
    private readonly GetInventoryReportUseCase _getInventoryReportUseCase;
    private readonly GetLowStockReportUseCase _getLowStockReportUseCase;
    private readonly GetStockMovementsReportUseCase _getStockMovementsReportUseCase;
    private readonly GetOrderSummaryReportUseCase _getOrderSummaryReportUseCase;
    private readonly GetOrderTrendReportUseCase _getOrderTrendReportUseCase;
    private readonly GetBestSellingProductsReportUseCase _getBestSellingProductsReportUseCase;
    private readonly GetSalesByCustomerReportUseCase _getSalesByCustomerReportUseCase;
    private readonly GetCancellationReportUseCase _getCancellationReportUseCase;

    public ReportsController(
        GetDashboardSummaryUseCase getDashboardSummaryUseCase,
        GetInventoryReportUseCase getInventoryReportUseCase,
        GetLowStockReportUseCase getLowStockReportUseCase,
        GetStockMovementsReportUseCase getStockMovementsReportUseCase,
        GetOrderSummaryReportUseCase getOrderSummaryReportUseCase,
        GetOrderTrendReportUseCase getOrderTrendReportUseCase,
        GetBestSellingProductsReportUseCase getBestSellingProductsReportUseCase,
        GetSalesByCustomerReportUseCase getSalesByCustomerReportUseCase,
        GetCancellationReportUseCase getCancellationReportUseCase)
    {
        _getDashboardSummaryUseCase = getDashboardSummaryUseCase;
        _getInventoryReportUseCase = getInventoryReportUseCase;
        _getLowStockReportUseCase = getLowStockReportUseCase;
        _getStockMovementsReportUseCase = getStockMovementsReportUseCase;
        _getOrderSummaryReportUseCase = getOrderSummaryReportUseCase;
        _getOrderTrendReportUseCase = getOrderTrendReportUseCase;
        _getBestSellingProductsReportUseCase = getBestSellingProductsReportUseCase;
        _getSalesByCustomerReportUseCase = getSalesByCustomerReportUseCase;
        _getCancellationReportUseCase = getCancellationReportUseCase;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboard(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        CancellationToken cancellationToken)
    {
        var summary = await _getDashboardSummaryUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            cancellationToken);

        return Ok(summary);
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(PagedResult<InventoryReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InventoryReportResponse>>> GetInventory(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLowStock,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var report = await _getInventoryReportUseCase.ExecuteAsync(
            productId,
            categoryId,
            isActive,
            isLowStock,
            page,
            pageSize,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("inventory/low-stock")]
    [ProducesResponseType(typeof(PagedResult<LowStockReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LowStockReportResponse>>> GetLowStock(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] LowStockSort? sortBy,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var report = await _getLowStockReportUseCase.ExecuteAsync(
            categoryId,
            isActive,
            sortBy,
            page,
            pageSize,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("inventory/movements")]
    [ProducesResponseType(typeof(StockMovementReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockMovementReportResponse>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? orderId,
        [FromQuery] StockMovementType? movementType,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        [FromQuery] ReportGroupBy? groupBy,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var report = await _getStockMovementsReportUseCase.ExecuteAsync(
            productId,
            supplierId,
            orderId,
            movementType,
            startDateUtc,
            endDateUtc,
            period,
            groupBy,
            page,
            pageSize,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("orders/summary")]
    [ProducesResponseType(typeof(OrderSummaryReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderSummaryReportResponse>> GetOrderSummary(
        [FromQuery] Guid? customerId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        CancellationToken cancellationToken)
    {
        var report = await _getOrderSummaryReportUseCase.ExecuteAsync(
            customerId,
            startDateUtc,
            endDateUtc,
            period,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("orders/trend")]
    [ProducesResponseType(typeof(OrderTrendReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderTrendReportResponse>> GetOrderTrend(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        [FromQuery] ReportGroupBy? groupBy,
        CancellationToken cancellationToken)
    {
        var report = await _getOrderTrendReportUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            groupBy,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("products/best-sellers")]
    [ProducesResponseType(typeof(IReadOnlyList<BestSellingProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BestSellingProductResponse>>> GetBestSellers(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var report = await _getBestSellingProductsReportUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            limit,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("sales/by-customer")]
    [ProducesResponseType(typeof(IReadOnlyList<SalesByCustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SalesByCustomerResponse>>> GetSalesByCustomer(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var report = await _getSalesByCustomerReportUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            limit,
            cancellationToken);

        return Ok(report);
    }

    [HttpGet("orders/cancellations")]
    [ProducesResponseType(typeof(CancellationReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CancellationReportResponse>> GetCancellations(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] ReportPeriodPreset? period,
        CancellationToken cancellationToken)
    {
        var report = await _getCancellationReportUseCase.ExecuteAsync(
            startDateUtc,
            endDateUtc,
            period,
            cancellationToken);

        return Ok(report);
    }
}
