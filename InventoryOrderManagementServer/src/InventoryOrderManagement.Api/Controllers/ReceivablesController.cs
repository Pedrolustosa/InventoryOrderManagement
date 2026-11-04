using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/finance/receivables")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ReceivablesController : ControllerBase
{
    private readonly CreateReceivableUseCase _create;
    private readonly GetAllReceivablesUseCase _getAll;
    private readonly GetReceivableByIdUseCase _getById;
    private readonly UpdateReceivableUseCase _update;
    private readonly CancelReceivableUseCase _cancel;
    private readonly SettleReceivableInstallmentUseCase _settle;

    public ReceivablesController(
        CreateReceivableUseCase create,
        GetAllReceivablesUseCase getAll,
        GetReceivableByIdUseCase getById,
        UpdateReceivableUseCase update,
        CancelReceivableUseCase cancel,
        SettleReceivableInstallmentUseCase settle)
    {
        _create = create;
        _getAll = getAll;
        _getById = getById;
        _update = update;
        _cancel = cancel;
        _settle = settle;
    }

    [Authorize(Policy = Permissions.Finance.ReceivablesCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(FinancialTitleResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<FinancialTitleResponse>> Create(
        [FromBody] CreateReceivableRequest request,
        CancellationToken cancellationToken)
    {
        var title = await _create.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = title.Id }, title);
    }

    [Authorize(Policy = Permissions.Finance.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FinancialTitleSummaryResponse>), StatusCodes.Status200OK)]
    public Task<PagedResult<FinancialTitleSummaryResponse>> GetAll(
        [FromQuery] FinancialTitleStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] string? documentNumber,
        [FromQuery] DateOnly? issueFrom,
        [FromQuery] DateOnly? issueTo,
        [FromQuery] DateOnly? dueFrom,
        [FromQuery] DateOnly? dueTo,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _getAll.ExecuteAsync(
            new FinancialTitleQuery
            {
                Status = status,
                CounterpartyId = customerId,
                DocumentNumber = documentNumber,
                IssueFrom = issueFrom,
                IssueTo = issueTo,
                DueFrom = dueFrom,
                DueTo = dueTo,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

    [Authorize(Policy = Permissions.Finance.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FinancialTitleResponse), StatusCodes.Status200OK)]
    public Task<FinancialTitleResponse> GetById(Guid id, CancellationToken cancellationToken) =>
        _getById.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Finance.ReceivablesUpdate)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FinancialTitleResponse), StatusCodes.Status200OK)]
    public Task<FinancialTitleResponse> Update(
        Guid id,
        [FromBody] UpdateFinancialTitleRequest request,
        CancellationToken cancellationToken) =>
        _update.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Finance.ReceivablesCancel)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(FinancialTitleResponse), StatusCodes.Status200OK)]
    public Task<FinancialTitleResponse> Cancel(
        Guid id,
        [FromBody] CancelFinancialTitleRequest request,
        CancellationToken cancellationToken) =>
        _cancel.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Finance.ReceivablesSettle)]
    [HttpPost("{id:guid}/installments/{installmentId:guid}/settlements")]
    [ProducesResponseType(typeof(FinancialSettlementResponse), StatusCodes.Status200OK)]
    public Task<FinancialSettlementResponse> Settle(
        Guid id,
        Guid installmentId,
        [FromBody] SettleInstallmentRequest request,
        CancellationToken cancellationToken) =>
        _settle.ExecuteAsync(id, installmentId, request, cancellationToken);
}
