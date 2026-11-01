using InventoryOrderManagement.Application.Customers;
using InventoryOrderManagement.Application.Customers.Dtos;
using InventoryOrderManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CreateCustomerUseCase _createCustomerUseCase;
    private readonly GetAllCustomersUseCase _getAllCustomersUseCase;
    private readonly GetCustomerByIdUseCase _getCustomerByIdUseCase;
    private readonly UpdateCustomerUseCase _updateCustomerUseCase;
    private readonly DeactivateCustomerUseCase _deactivateCustomerUseCase;

    public CustomersController(
        CreateCustomerUseCase createCustomerUseCase,
        GetAllCustomersUseCase getAllCustomersUseCase,
        GetCustomerByIdUseCase getCustomerByIdUseCase,
        UpdateCustomerUseCase updateCustomerUseCase,
        DeactivateCustomerUseCase deactivateCustomerUseCase)
    {
        _createCustomerUseCase = createCustomerUseCase;
        _getAllCustomersUseCase = getAllCustomersUseCase;
        _getCustomerByIdUseCase = getCustomerByIdUseCase;
        _updateCustomerUseCase = updateCustomerUseCase;
        _deactivateCustomerUseCase = deactivateCustomerUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _createCustomerUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.Id },
            customer);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll(
        [FromQuery] bool? isActive,
        [FromQuery] string? name,
        [FromQuery] CustomerType? customerType,
        CancellationToken cancellationToken)
    {
        var customers = await _getAllCustomersUseCase.ExecuteAsync(
            isActive,
            name,
            customerType,
            cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await _getCustomerByIdUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(customer);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        await _updateCustomerUseCase.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _deactivateCustomerUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
