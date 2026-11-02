using InventoryOrderManagement.Application.Finance;
using InventoryOrderManagement.Application.Finance.Dtos;
using InventoryOrderManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/finance/cash-accounts")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class CashAccountsController : ControllerBase
{
    private readonly CreateCashAccountUseCase _create;
    private readonly GetAllCashAccountsUseCase _getAll;
    private readonly GetCashAccountByIdUseCase _getById;
    private readonly UpdateCashAccountUseCase _update;
    private readonly DeactivateCashAccountUseCase _deactivate;
    private readonly GetCashAccountBalanceUseCase _balance;

    public CashAccountsController(
        CreateCashAccountUseCase create,
        GetAllCashAccountsUseCase getAll,
        GetCashAccountByIdUseCase getById,
        UpdateCashAccountUseCase update,
        DeactivateCashAccountUseCase deactivate,
        GetCashAccountBalanceUseCase balance)
    {
        _create = create;
        _getAll = getAll;
        _getById = getById;
        _update = update;
        _deactivate = deactivate;
        _balance = balance;
    }

    [Authorize(Policy = Permissions.Finance.CashAccountsManage)]
    [HttpPost]
    [ProducesResponseType(typeof(CashAccountResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashAccountResponse>> Create(
        [FromBody] CreateCashAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _create.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [Authorize(Policy = Permissions.Finance.CashAccountsView)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CashAccountResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CashAccountResponse>> GetAll(CancellationToken cancellationToken) =>
        _getAll.ExecuteAsync(cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashAccountsView)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CashAccountResponse), StatusCodes.Status200OK)]
    public Task<CashAccountResponse> GetById(Guid id, CancellationToken cancellationToken) =>
        _getById.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashAccountsManage)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CashAccountResponse), StatusCodes.Status200OK)]
    public Task<CashAccountResponse> Update(
        Guid id,
        [FromBody] UpdateCashAccountRequest request,
        CancellationToken cancellationToken) =>
        _update.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashAccountsManage)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(CashAccountResponse), StatusCodes.Status200OK)]
    public Task<CashAccountResponse> Deactivate(Guid id, CancellationToken cancellationToken) =>
        _deactivate.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Finance.CashAccountsView)]
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(CashAccountBalanceResponse), StatusCodes.Status200OK)]
    public Task<CashAccountBalanceResponse> Balance(Guid id, CancellationToken cancellationToken) =>
        _balance.ExecuteAsync(id, cancellationToken);
}
