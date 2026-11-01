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
    private readonly ShipOrderUseCase _shipOrderUseCase;
    private readonly DeliverOrderUseCase _deliverOrderUseCase;
    private readonly CancelOrderUseCase _cancelOrderUseCase;

    public OrdersController(
        CreateOrderUseCase createOrderUseCase,
        GetAllOrdersUseCase getAllOrdersUseCase,
        GetOrderByIdUseCase getOrderByIdUseCase,
        UpdateDraftOrderUseCase updateDraftOrderUseCase,
        ConfirmOrderUseCase confirmOrderUseCase,
        ShipOrderUseCase shipOrderUseCase,
        DeliverOrderUseCase deliverOrderUseCase,
        CancelOrderUseCase cancelOrderUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
        _getAllOrdersUseCase = getAllOrdersUseCase;
        _getOrderByIdUseCase = getOrderByIdUseCase;
        _updateDraftOrderUseCase = updateDraftOrderUseCase;
        _confirmOrderUseCase = confirmOrderUseCase;
        _shipOrderUseCase = shipOrderUseCase;
        _deliverOrderUseCase = deliverOrderUseCase;
        _cancelOrderUseCase = cancelOrderUseCase;
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

    [Authorize(Policy = Permissions.Orders.Ship)]
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Ship(Guid id, CancellationToken cancellationToken)
    {
        var order = await _shipOrderUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(order);
    }

    [Authorize(Policy = Permissions.Orders.Deliver)]
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var order = await _deliverOrderUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(order);
    }

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
}
