using InventoryOrderManagement.Application.Reports.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Application.Shipments;
using InventoryOrderManagement.Application.Shipments.Dtos;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ShipmentsController : ControllerBase
{
    private readonly CreateShipmentUseCase _createShipmentUseCase;
    private readonly UpdateShipmentUseCase _updateShipmentUseCase;
    private readonly GetAllShipmentsUseCase _getAllShipmentsUseCase;
    private readonly GetShipmentByIdUseCase _getShipmentByIdUseCase;
    private readonly GetShipmentHistoryUseCase _getShipmentHistoryUseCase;
    private readonly DispatchShipmentUseCase _dispatchShipmentUseCase;
    private readonly SetShipmentInTransitUseCase _setShipmentInTransitUseCase;
    private readonly MarkShipmentDeliveredUseCase _markShipmentDeliveredUseCase;
    private readonly MarkShipmentFailedUseCase _markShipmentFailedUseCase;
    private readonly ResumeShipmentUseCase _resumeShipmentUseCase;

    public ShipmentsController(
        CreateShipmentUseCase createShipmentUseCase,
        UpdateShipmentUseCase updateShipmentUseCase,
        GetAllShipmentsUseCase getAllShipmentsUseCase,
        GetShipmentByIdUseCase getShipmentByIdUseCase,
        GetShipmentHistoryUseCase getShipmentHistoryUseCase,
        DispatchShipmentUseCase dispatchShipmentUseCase,
        SetShipmentInTransitUseCase setShipmentInTransitUseCase,
        MarkShipmentDeliveredUseCase markShipmentDeliveredUseCase,
        MarkShipmentFailedUseCase markShipmentFailedUseCase,
        ResumeShipmentUseCase resumeShipmentUseCase)
    {
        _createShipmentUseCase = createShipmentUseCase;
        _updateShipmentUseCase = updateShipmentUseCase;
        _getAllShipmentsUseCase = getAllShipmentsUseCase;
        _getShipmentByIdUseCase = getShipmentByIdUseCase;
        _getShipmentHistoryUseCase = getShipmentHistoryUseCase;
        _dispatchShipmentUseCase = dispatchShipmentUseCase;
        _setShipmentInTransitUseCase = setShipmentInTransitUseCase;
        _markShipmentDeliveredUseCase = markShipmentDeliveredUseCase;
        _markShipmentFailedUseCase = markShipmentFailedUseCase;
        _resumeShipmentUseCase = resumeShipmentUseCase;
    }

    [Authorize(Policy = Permissions.Shipments.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShipmentResponse>> Create(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _createShipmentUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = Permissions.Shipments.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShipmentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ShipmentSummaryResponse>> GetAll(
        [FromQuery] ShipmentStatus? status,
        [FromQuery] Guid? orderId,
        [FromQuery] Guid? customerId,
        [FromQuery] string? carrierName,
        [FromQuery] string? trackingCode,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return _getAllShipmentsUseCase.ExecuteAsync(
            new ShipmentListQuery
            {
                Status = status,
                OrderId = orderId,
                CustomerId = customerId,
                CarrierName = carrierName,
                TrackingCode = trackingCode,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);
    }

    [Authorize(Policy = Permissions.Shipments.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ShipmentResponse> GetById(Guid id, CancellationToken cancellationToken)
        => _getShipmentByIdUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.View)]
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipmentStatusHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ShipmentStatusHistoryResponse>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
        => _getShipmentHistoryUseCase.ExecuteAsync(id, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> Update(
        Guid id,
        [FromBody] UpdateShipmentRequest request,
        CancellationToken cancellationToken)
        => _updateShipmentUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Dispatch)]
    [HttpPost("{id:guid}/dispatch")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> Dispatch(
        Guid id,
        [FromBody] ShipmentNotesRequest request,
        CancellationToken cancellationToken)
        => _dispatchShipmentUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Advance)]
    [HttpPost("{id:guid}/in-transit")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> SetInTransit(
        Guid id,
        [FromBody] ShipmentNotesRequest request,
        CancellationToken cancellationToken)
        => _setShipmentInTransitUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Advance)]
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> Deliver(
        Guid id,
        [FromBody] ShipmentNotesRequest request,
        CancellationToken cancellationToken)
        => _markShipmentDeliveredUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Advance)]
    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> Fail(
        Guid id,
        [FromBody] FailShipmentRequest request,
        CancellationToken cancellationToken)
        => _markShipmentFailedUseCase.ExecuteAsync(id, request, cancellationToken);

    [Authorize(Policy = Permissions.Shipments.Advance)]
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ShipmentResponse> Resume(
        Guid id,
        [FromBody] ShipmentNotesRequest request,
        CancellationToken cancellationToken)
        => _resumeShipmentUseCase.ExecuteAsync(id, request, cancellationToken);
}
