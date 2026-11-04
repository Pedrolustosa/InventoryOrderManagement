using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly CreatePurchaseOrderUseCase _createPurchaseOrderUseCase;
    private readonly GetAllPurchaseOrdersUseCase _getAllPurchaseOrdersUseCase;
    private readonly GetPurchaseOrderByIdUseCase _getPurchaseOrderByIdUseCase;
    private readonly UpdatePurchaseOrderUseCase _updatePurchaseOrderUseCase;
    private readonly SendPurchaseOrderUseCase _sendPurchaseOrderUseCase;
    private readonly ReceivePurchaseOrderUseCase _receivePurchaseOrderUseCase;
    private readonly GetPurchaseReceiptsUseCase _getPurchaseReceiptsUseCase;
    private readonly CancelPurchaseOrderUseCase _cancelPurchaseOrderUseCase;

    public PurchaseOrdersController(
        CreatePurchaseOrderUseCase createPurchaseOrderUseCase,
        GetAllPurchaseOrdersUseCase getAllPurchaseOrdersUseCase,
        GetPurchaseOrderByIdUseCase getPurchaseOrderByIdUseCase,
        UpdatePurchaseOrderUseCase updatePurchaseOrderUseCase,
        SendPurchaseOrderUseCase sendPurchaseOrderUseCase,
        ReceivePurchaseOrderUseCase receivePurchaseOrderUseCase,
        GetPurchaseReceiptsUseCase getPurchaseReceiptsUseCase,
        CancelPurchaseOrderUseCase cancelPurchaseOrderUseCase)
    {
        _createPurchaseOrderUseCase = createPurchaseOrderUseCase;
        _getAllPurchaseOrdersUseCase = getAllPurchaseOrdersUseCase;
        _getPurchaseOrderByIdUseCase = getPurchaseOrderByIdUseCase;
        _updatePurchaseOrderUseCase = updatePurchaseOrderUseCase;
        _sendPurchaseOrderUseCase = sendPurchaseOrderUseCase;
        _receivePurchaseOrderUseCase = receivePurchaseOrderUseCase;
        _getPurchaseReceiptsUseCase = getPurchaseReceiptsUseCase;
        _cancelPurchaseOrderUseCase = cancelPurchaseOrderUseCase;
    }

    [Authorize(Policy = Permissions.Purchases.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Create(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _createPurchaseOrderUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [Authorize(Policy = Permissions.Purchases.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseOrderSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PurchaseOrderSummaryResponse>>> GetAll(
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] string? purchaseOrderNumber,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var orders = await _getAllPurchaseOrdersUseCase.ExecuteAsync(
            new PurchaseOrderQuery
            {
                Status = status,
                SupplierId = supplierId,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc,
                PurchaseOrderNumber = purchaseOrderNumber,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            },
            cancellationToken);

        return Ok(orders);
    }

    [Authorize(Policy = Permissions.Purchases.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _getPurchaseOrderByIdUseCase.ExecuteAsync(id, cancellationToken));
    }

    [Authorize(Policy = Permissions.Purchases.UpdateDraft)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Update(
        Guid id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updatePurchaseOrderUseCase.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Policy = Permissions.Purchases.Send)]
    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Send(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sendPurchaseOrderUseCase.ExecuteAsync(id, cancellationToken));
    }

    [Authorize(Policy = Permissions.Purchases.Receive)]
    [HttpPost("{id:guid}/receipts")]
    [ProducesResponseType(typeof(PurchaseReceiptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseReceiptResponse>> Receive(
        Guid id,
        [FromBody] ReceivePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = await _receivePurchaseOrderUseCase.ExecuteAsync(id, request, cancellationToken);
        return CreatedAtAction(
            nameof(PurchaseReceiptsController.GetById),
            "PurchaseReceipts",
            new { receiptId = receipt.Id },
            receipt);
    }

    [Authorize(Policy = Permissions.Purchases.View)]
    [HttpGet("{id:guid}/receipts")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseReceiptResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PurchaseReceiptResponse>>> GetReceipts(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _getPurchaseReceiptsUseCase.ExecuteAsync(id, cancellationToken));
    }

    [Authorize(Policy = Permissions.Purchases.Cancel)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Cancel(
        Guid id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cancelPurchaseOrderUseCase.ExecuteAsync(id, request, cancellationToken));
    }
}

[ApiController]
[Route("api/purchase-receipts")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class PurchaseReceiptsController : ControllerBase
{
    private readonly GetPurchaseReceiptByIdUseCase _getPurchaseReceiptByIdUseCase;

    public PurchaseReceiptsController(GetPurchaseReceiptByIdUseCase getPurchaseReceiptByIdUseCase)
    {
        _getPurchaseReceiptByIdUseCase = getPurchaseReceiptByIdUseCase;
    }

    [Authorize(Policy = Permissions.Purchases.View)]
    [HttpGet("{receiptId:guid}")]
    [ProducesResponseType(typeof(PurchaseReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseReceiptResponse>> GetById(Guid receiptId, CancellationToken cancellationToken)
    {
        return Ok(await _getPurchaseReceiptByIdUseCase.ExecuteAsync(receiptId, cancellationToken));
    }
}
