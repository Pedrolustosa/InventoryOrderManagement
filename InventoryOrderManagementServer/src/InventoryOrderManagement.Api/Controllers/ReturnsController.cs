using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Application.Returns;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityPagedResult = InventoryOrderManagement.Application.Identity.Dtos.PagedResult<InventoryOrderManagement.Application.Identity.Dtos.SecurityAuditLogResponse>;
using ReportPagedResult = InventoryOrderManagement.Application.Reports.Dtos.PagedResult<InventoryOrderManagement.Application.Returns.Dtos.ReturnSummaryResponse>;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/returns")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ReturnsController : ControllerBase
{
    private readonly CreateCustomerReturnUseCase _createCustomerReturnUseCase;
    private readonly CreateSupplierReturnUseCase _createSupplierReturnUseCase;
    private readonly GetAllReturnsUseCase _getAllReturnsUseCase;
    private readonly GetReturnByIdUseCase _getReturnByIdUseCase;
    private readonly UpdateReturnUseCase _updateReturnUseCase;
    private readonly GetCustomerReturnableItemsUseCase _getCustomerReturnableItemsUseCase;
    private readonly GetSupplierReturnableItemsUseCase _getSupplierReturnableItemsUseCase;
    private readonly ReceiveCustomerReturnUseCase _receiveCustomerReturnUseCase;
    private readonly StartReturnInspectionUseCase _startReturnInspectionUseCase;
    private readonly CompleteCustomerReturnInspectionUseCase _completeCustomerReturnInspectionUseCase;
    private readonly AuthorizeSupplierReturnUseCase _authorizeSupplierReturnUseCase;
    private readonly DispatchSupplierReturnUseCase _dispatchSupplierReturnUseCase;
    private readonly CancelReturnUseCase _cancelReturnUseCase;
    private readonly GetReturnHistoryUseCase _getReturnHistoryUseCase;
    private readonly GetReturnStockMovementsUseCase _getReturnStockMovementsUseCase;

    public ReturnsController(
        CreateCustomerReturnUseCase createCustomerReturnUseCase,
        CreateSupplierReturnUseCase createSupplierReturnUseCase,
        GetAllReturnsUseCase getAllReturnsUseCase,
        GetReturnByIdUseCase getReturnByIdUseCase,
        UpdateReturnUseCase updateReturnUseCase,
        GetCustomerReturnableItemsUseCase getCustomerReturnableItemsUseCase,
        GetSupplierReturnableItemsUseCase getSupplierReturnableItemsUseCase,
        ReceiveCustomerReturnUseCase receiveCustomerReturnUseCase,
        StartReturnInspectionUseCase startReturnInspectionUseCase,
        CompleteCustomerReturnInspectionUseCase completeCustomerReturnInspectionUseCase,
        AuthorizeSupplierReturnUseCase authorizeSupplierReturnUseCase,
        DispatchSupplierReturnUseCase dispatchSupplierReturnUseCase,
        CancelReturnUseCase cancelReturnUseCase,
        GetReturnHistoryUseCase getReturnHistoryUseCase,
        GetReturnStockMovementsUseCase getReturnStockMovementsUseCase)
    {
        _createCustomerReturnUseCase = createCustomerReturnUseCase;
        _createSupplierReturnUseCase = createSupplierReturnUseCase;
        _getAllReturnsUseCase = getAllReturnsUseCase;
        _getReturnByIdUseCase = getReturnByIdUseCase;
        _updateReturnUseCase = updateReturnUseCase;
        _getCustomerReturnableItemsUseCase = getCustomerReturnableItemsUseCase;
        _getSupplierReturnableItemsUseCase = getSupplierReturnableItemsUseCase;
        _receiveCustomerReturnUseCase = receiveCustomerReturnUseCase;
        _startReturnInspectionUseCase = startReturnInspectionUseCase;
        _completeCustomerReturnInspectionUseCase = completeCustomerReturnInspectionUseCase;
        _authorizeSupplierReturnUseCase = authorizeSupplierReturnUseCase;
        _dispatchSupplierReturnUseCase = dispatchSupplierReturnUseCase;
        _cancelReturnUseCase = cancelReturnUseCase;
        _getReturnHistoryUseCase = getReturnHistoryUseCase;
        _getReturnStockMovementsUseCase = getReturnStockMovementsUseCase;
    }

    [Authorize(Policy = Permissions.Returns.CustomerCreate)]
    [HttpPost("customer")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnResponse>> CreateCustomer(
        [FromBody] CreateCustomerReturnRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _createCustomerReturnUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = Permissions.Returns.SupplierCreate)]
    [HttpPost("supplier")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnResponse>> CreateSupplier(
        [FromBody] CreateSupplierReturnRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _createSupplierReturnUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet("customer/eligible-orders/{orderId:guid}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnableItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IReadOnlyList<ReturnableItemResponse>> GetCustomerEligibleItems(
        Guid orderId,
        CancellationToken cancellationToken)
        => _getCustomerReturnableItemsUseCase.ExecuteAsync(orderId, cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet("supplier/eligible-orders/{purchaseOrderId:guid}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnableItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IReadOnlyList<ReturnableItemResponse>> GetSupplierEligibleItems(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
        => _getSupplierReturnableItemsUseCase.ExecuteAsync(purchaseOrderId, cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet]
    [ProducesResponseType(typeof(ReportPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<ReportPagedResult> GetAll(
        [FromQuery] ReturnType? type,
        [FromQuery] ReturnStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? orderId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] string? returnNumber,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => _getAllReturnsUseCase.ExecuteAsync(
            new ReturnListQuery
            {
                Type = type,
                Status = status,
                CustomerId = customerId,
                SupplierId = supplierId,
                OrderId = orderId,
                PurchaseOrderId = purchaseOrderId,
                ReturnNumber = returnNumber,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ReturnResponse> GetById(Guid id, CancellationToken cancellationToken)
        => _getReturnByIdUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IdentityPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IdentityPagedResult> GetHistory(
        Guid id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => _getReturnHistoryUseCase.ExecuteAsync(id, page, pageSize, cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpGet("{id:guid}/stock-movements")]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnStockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ReturnStockMovementResponse>> GetStockMovements(
        Guid id,
        CancellationToken cancellationToken)
        => _getReturnStockMovementsUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Returns.View)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> Update(
        Guid id,
        [FromBody] UpdateReturnRequest request,
        CancellationToken cancellationToken)
        => _updateReturnUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Returns.CustomerReceive)]
    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> Receive(
        Guid id,
        [FromBody] ReceiveCustomerReturnRequest request,
        CancellationToken cancellationToken)
        => _receiveCustomerReturnUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Returns.CustomerInspect)]
    [HttpPost("{id:guid}/inspection/start")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> StartInspection(Guid id, CancellationToken cancellationToken)
        => _startReturnInspectionUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Returns.CustomerInspect)]
    [HttpPost("{id:guid}/inspection/complete")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> CompleteInspection(
        Guid id,
        [FromBody] CompleteReturnInspectionRequest request,
        CancellationToken cancellationToken)
        => _completeCustomerReturnInspectionUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Returns.SupplierAuthorize)]
    [HttpPost("{id:guid}/authorize")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> Authorize(
        Guid id,
        [FromBody] AuthorizeSupplierReturnRequest request,
        CancellationToken cancellationToken)
        => _authorizeSupplierReturnUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Returns.SupplierDispatch)]
    [HttpPost("{id:guid}/dispatch")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> Dispatch(
        Guid id,
        [FromBody] DispatchSupplierReturnRequest request,
        CancellationToken cancellationToken)
        => _dispatchSupplierReturnUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Returns.Cancel)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ReturnResponse> Cancel(
        Guid id,
        [FromBody] CancelReturnRequest request,
        CancellationToken cancellationToken)
        => _cancelReturnUseCase.ExecuteAsync(id, request, cancellationToken);
}
