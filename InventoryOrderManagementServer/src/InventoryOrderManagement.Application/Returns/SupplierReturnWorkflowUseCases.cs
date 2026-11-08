using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Abstractions.Notifications;
using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Notifications;
using InventoryOrderManagement.Application.Returns.Dtos;
using InventoryOrderManagement.Application.Security;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Application.Returns;

public sealed class AuthorizeSupplierReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public AuthorizeSupplierReturnUseCase(
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        AuthorizeSupplierReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.Authorize(request.Notes, request.AuthorizationReference);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.SupplierReturnAuthorized,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class DispatchSupplierReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public DispatchSupplierReturnUseCase(
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        DispatchSupplierReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Stock, purchase-order return quantities and finance are applied by ReturnCompletedHandler in this
        // same transaction when the dispatch completes the return.
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            if (entity.Type != ReturnType.SupplierReturn)
            {
                throw new DomainException("This operation applies only to supplier returns.");
            }

            entity.Dispatch(request.Notes, request.DispatchReference);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.SupplierReturnDispatched,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}

public sealed class CancelReturnUseCase
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecurityAuditWriter _auditWriter;
    private readonly ICurrentUser _currentUser;

    public CancelReturnUseCase(
        IReturnRequestRepository returnRepository,
        IUnitOfWork unitOfWork,
        ISecurityAuditWriter auditWriter,
        ICurrentUser currentUser)
    {
        _returnRepository = returnRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _currentUser = currentUser;
    }

    public async Task<ReturnResponse> ExecuteAsync(
        Guid returnRequestId,
        CancelReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var entity = await _returnRepository.GetTrackedByIdAsync(returnRequestId, ct)
                ?? throw new ReturnNotFoundException(returnRequestId);

            entity.Cancel(request.Reason);
        }, cancellationToken);

        var updated = await _returnRepository.GetByIdAsync(returnRequestId, cancellationToken)
            ?? throw new ReturnNotFoundException(returnRequestId);

        await ReturnAudit.WriteAsync(
            _auditWriter,
            _currentUser,
            SecurityActions.ReturnCancelled,
            updated.Id,
            cancellationToken);

        return ReturnResponse.FromEntity(updated);
    }
}
