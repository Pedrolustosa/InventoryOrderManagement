using InventoryOrderManagement.Application.Inventory;
using InventoryOrderManagement.Application.Inventory.Dtos;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly ReceiveStockUseCase _receiveStockUseCase;
    private readonly RegisterStockExitUseCase _registerStockExitUseCase;
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly UpdateMinimumStockLevelUseCase _updateMinimumStockLevelUseCase;
    private readonly GetInventoryBalanceByProductUseCase _getInventoryBalanceByProductUseCase;
    private readonly GetAllInventoryBalancesUseCase _getAllInventoryBalancesUseCase;
    private readonly GetLowStockProductsUseCase _getLowStockProductsUseCase;
    private readonly GetStockMovementsUseCase _getStockMovementsUseCase;

    public InventoryController(
        ReceiveStockUseCase receiveStockUseCase,
        RegisterStockExitUseCase registerStockExitUseCase,
        AdjustStockUseCase adjustStockUseCase,
        UpdateMinimumStockLevelUseCase updateMinimumStockLevelUseCase,
        GetInventoryBalanceByProductUseCase getInventoryBalanceByProductUseCase,
        GetAllInventoryBalancesUseCase getAllInventoryBalancesUseCase,
        GetLowStockProductsUseCase getLowStockProductsUseCase,
        GetStockMovementsUseCase getStockMovementsUseCase)
    {
        _receiveStockUseCase = receiveStockUseCase;
        _registerStockExitUseCase = registerStockExitUseCase;
        _adjustStockUseCase = adjustStockUseCase;
        _updateMinimumStockLevelUseCase = updateMinimumStockLevelUseCase;
        _getInventoryBalanceByProductUseCase = getInventoryBalanceByProductUseCase;
        _getAllInventoryBalancesUseCase = getAllInventoryBalancesUseCase;
        _getLowStockProductsUseCase = getLowStockProductsUseCase;
        _getStockMovementsUseCase = getStockMovementsUseCase;
    }

    [HttpPost("entries")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockMovementResponse>> Receive(
        [FromBody] ReceiveStockRequest request,
        CancellationToken cancellationToken)
    {
        var movement = await _receiveStockUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetBalance),
            new { productId = movement.ProductId },
            movement);
    }

    [HttpPost("exits")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockMovementResponse>> RegisterExit(
        [FromBody] RegisterStockExitRequest request,
        CancellationToken cancellationToken)
    {
        var movement = await _registerStockExitUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetBalance),
            new { productId = movement.ProductId },
            movement);
    }

    [HttpPost("adjustments")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(InventoryBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Adjust(
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adjustStockUseCase.ExecuteAsync(request, cancellationToken);

        if (!result.MovementCreated)
        {
            return Ok(result.Balance);
        }

        return CreatedAtAction(
            nameof(GetBalance),
            new { productId = result.Movement!.ProductId },
            result.Movement);
    }

    [HttpGet("balances/{productId:guid}")]
    [ProducesResponseType(typeof(InventoryBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryBalanceResponse>> GetBalance(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var balance = await _getInventoryBalanceByProductUseCase.ExecuteAsync(productId, cancellationToken);
        return Ok(balance);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryBalanceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InventoryBalanceResponse>>> GetBalances(
        [FromQuery] Guid? productId,
        [FromQuery] bool? isLowStock,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var balances = await _getAllInventoryBalancesUseCase.ExecuteAsync(
            productId,
            isLowStock,
            isActive,
            cancellationToken);

        return Ok(balances);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LowStockProductResponse>>> GetLowStock(
        CancellationToken cancellationToken)
    {
        var products = await _getLowStockProductsUseCase.ExecuteAsync(cancellationToken);
        return Ok(products);
    }

    [HttpPut("balances/{productId:guid}/minimum-stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMinimumStock(
        Guid productId,
        [FromBody] UpdateMinimumStockLevelRequest request,
        CancellationToken cancellationToken)
    {
        await _updateMinimumStockLevelUseCase.ExecuteAsync(productId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("movements")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? supplierId,
        [FromQuery] StockMovementType? movementType,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken)
    {
        var movements = await _getStockMovementsUseCase.ExecuteAsync(
            productId,
            supplierId,
            movementType,
            startDateUtc,
            endDateUtc,
            cancellationToken);

        return Ok(movements);
    }

    [HttpGet("products/{productId:guid}/movements")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> GetMovementsByProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var movements = await _getStockMovementsUseCase.ExecuteForProductAsync(productId, cancellationToken);
        return Ok(movements);
    }
}
