using InventoryOrderManagement.Application.Orders;
using InventoryOrderManagement.Application.Orders.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/orders")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly GetAllOrdersUseCase _getAllOrdersUseCase;
    private readonly GetOrderByIdUseCase _getOrderByIdUseCase;
    private readonly UpdateDraftOrderUseCase _updateDraftOrderUseCase;
    private readonly ConfirmOrderUseCase _confirmOrderUseCase;
    private readonly CancelOrderUseCase _cancelOrderUseCase;
    private readonly GetOrderTimelineUseCase _getOrderTimelineUseCase;

    public OrdersController(
        CreateOrderUseCase createOrderUseCase,
        GetAllOrdersUseCase getAllOrdersUseCase,
        GetOrderByIdUseCase getOrderByIdUseCase,
        UpdateDraftOrderUseCase updateDraftOrderUseCase,
        ConfirmOrderUseCase confirmOrderUseCase,
        CancelOrderUseCase cancelOrderUseCase,
        GetOrderTimelineUseCase getOrderTimelineUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
        _getAllOrdersUseCase = getAllOrdersUseCase;
        _getOrderByIdUseCase = getOrderByIdUseCase;
        _updateDraftOrderUseCase = updateDraftOrderUseCase;
        _confirmOrderUseCase = confirmOrderUseCase;
        _cancelOrderUseCase = cancelOrderUseCase;
        _getOrderTimelineUseCase = getOrderTimelineUseCase;
    }

    [Authorize(Policy = Permissions.Orders.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(OrderCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderCreatedResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _createOrderUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [Authorize(Policy = Permissions.Orders.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] string? orderNumber,
        CancellationToken cancellationToken)
    {
        var orders = await _getAllOrdersUseCase.ExecuteAsync(
            new OrderQuery
            {
                CustomerId = customerId,
                Status = status,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc,
                OrderNumber = orderNumber
            },
            cancellationToken);

        return Ok(orders);
    }

    [Authorize(Policy = Permissions.Orders.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _getOrderByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(order);
    }

    /// <summary>
    /// Chronological history of the order across order, stock, shipment, finance and returns. Entries carry
    /// stable event codes for the client to translate. Finance entries require Finance.View and return entries
    /// Returns.View; they are omitted otherwise.
    /// </summary>
    [Authorize(Policy = Permissions.Orders.View)]
    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(OrderTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderTimelineDto>> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var timeline = await _getOrderTimelineUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(timeline);
    }

    [Authorize(Policy = Permissions.Orders.UpdateDraft)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDraftOrderRequest request,
        CancellationToken cancellationToken)
    {
        await _updateDraftOrderUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.Orders.Confirm)]
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var order = await _confirmOrderUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(order);
    }

    /// <summary>
    /// Removed: an order is shipped by dispatching its shipment (POST /api/shipments/{id}/dispatch).
    /// Kept for one version as 410 Gone so old clients get an actionable error instead of a 404.
    /// </summary>
    [Obsolete(LegacyShippingRemoval)]
    [Authorize]
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Ship(Guid id) =>
        LegacyShippingGone("POST /api/shipments to create the shipment, then POST /api/shipments/{shipmentId}/dispatch");

    /// <summary>
    /// Removed: an order is delivered by confirming its shipment delivery (POST /api/shipments/{id}/deliver).
    /// </summary>
    [Obsolete(LegacyShippingRemoval)]
    [Authorize]
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Deliver(Guid id) =>
        LegacyShippingGone("POST /api/shipments/{shipmentId}/deliver");

    [Authorize(Policy = Permissions.Orders.Cancel)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _cancelOrderUseCase.ExecuteAsync(id, request, cancellationToken);
        return Ok(order);
    }

    private const string LegacyShippingRemoval =
        "Legacy order ship/deliver was removed; remove these 410 endpoints in the next major version.";

    private ObjectResult LegacyShippingGone(string replacement)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "Endpoint removed",
            Detail = $"Orders are shipped and delivered only through their shipment. Use {replacement}.",
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["replacement"] = "/api/shipments";
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status410Gone };
    }
}
