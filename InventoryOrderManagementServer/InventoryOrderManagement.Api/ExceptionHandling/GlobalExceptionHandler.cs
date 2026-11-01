using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderManagement.Api.ExceptionHandling;

/// <summary>
/// Centralizes exception-to-HTTP mapping using ProblemDetails (RFC 7807).
/// Controllers stay free of try/catch boilerplate.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled application exception: {Title}", title);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                "Invalid request data",
                domainException.Message),
            InactiveCategoryException inactiveCategoryException => (
                StatusCodes.Status400BadRequest,
                "Inactive category",
                inactiveCategoryException.Message),
            CategoryNotFoundException categoryNotFoundException => (
                StatusCodes.Status404NotFound,
                "Category not found",
                categoryNotFoundException.Message),
            DuplicateSkuException duplicateSkuException => (
                StatusCodes.Status409Conflict,
                "Duplicate SKU",
                duplicateSkuException.Message),
            DuplicateCategoryNameException duplicateCategoryNameException => (
                StatusCodes.Status409Conflict,
                "Duplicate category name",
                duplicateCategoryNameException.Message),
            CategoryHasActiveProductsException categoryHasActiveProductsException => (
                StatusCodes.Status409Conflict,
                "Category has active products",
                categoryHasActiveProductsException.Message),
            InactiveSupplierException inactiveSupplierException => (
                StatusCodes.Status400BadRequest,
                "Inactive supplier",
                inactiveSupplierException.Message),
            SupplierNotFoundException supplierNotFoundException => (
                StatusCodes.Status404NotFound,
                "Supplier not found",
                supplierNotFoundException.Message),
            DuplicateTaxIdentifierException duplicateTaxIdentifierException => (
                StatusCodes.Status409Conflict,
                "Duplicate tax identifier",
                duplicateTaxIdentifierException.Message),
            InactiveCustomerException inactiveCustomerException => (
                StatusCodes.Status400BadRequest,
                "Inactive customer",
                inactiveCustomerException.Message),
            CustomerNotFoundException customerNotFoundException => (
                StatusCodes.Status404NotFound,
                "Customer not found",
                customerNotFoundException.Message),
            DuplicateCustomerTaxIdentifierException duplicateCustomerTaxIdentifierException => (
                StatusCodes.Status409Conflict,
                "Duplicate tax identifier",
                duplicateCustomerTaxIdentifierException.Message),
            ProductNotFoundException productNotFoundException => (
                StatusCodes.Status404NotFound,
                "Product not found",
                productNotFoundException.Message),
            InventoryBalanceNotFoundException inventoryBalanceNotFoundException => (
                StatusCodes.Status404NotFound,
                "Inventory balance not found",
                inventoryBalanceNotFoundException.Message),
            InvalidMovementPeriodException invalidMovementPeriodException => (
                StatusCodes.Status400BadRequest,
                "Invalid movement period",
                invalidMovementPeriodException.Message),
            InactiveProductStockEntryException inactiveProductStockEntryException => (
                StatusCodes.Status409Conflict,
                "Inactive product",
                inactiveProductStockEntryException.Message),
            InactiveSupplierStockEntryException inactiveSupplierStockEntryException => (
                StatusCodes.Status409Conflict,
                "Inactive supplier",
                inactiveSupplierStockEntryException.Message),
            InsufficientStockException insufficientStockException => (
                StatusCodes.Status409Conflict,
                "Insufficient stock",
                insufficientStockException.Message),
            InventoryConcurrencyException inventoryConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Inventory concurrency conflict",
                inventoryConcurrencyException.Message),
            OrderNotFoundException orderNotFoundException => (
                StatusCodes.Status404NotFound,
                "Order not found",
                orderNotFoundException.Message),
            InactiveCustomerOrderException inactiveCustomerOrderException => (
                StatusCodes.Status409Conflict,
                "Inactive customer",
                inactiveCustomerOrderException.Message),
            InactiveProductOrderException inactiveProductOrderException => (
                StatusCodes.Status409Conflict,
                "Inactive product",
                inactiveProductOrderException.Message),
            OrderInsufficientStockException orderInsufficientStockException => (
                StatusCodes.Status409Conflict,
                "Insufficient stock",
                orderInsufficientStockException.Message),
            InvalidOrderStatusException invalidOrderStatusException => (
                StatusCodes.Status409Conflict,
                "Invalid order status",
                invalidOrderStatusException.Message),
            InvalidOrderPeriodException invalidOrderPeriodException => (
                StatusCodes.Status400BadRequest,
                "Invalid order period",
                invalidOrderPeriodException.Message),
            InvalidReportRequestException invalidReportRequestException => (
                StatusCodes.Status400BadRequest,
                "Invalid report request",
                invalidReportRequestException.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };

    }
}
