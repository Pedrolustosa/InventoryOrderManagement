using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Application.Exceptions;
using InventoryOrderManagement.Application.Identity.Dtos;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class SecurityAuditQueryService : ISecurityAuditQuery
{
    private readonly InventoryDbContext _dbContext;

    public SecurityAuditQueryService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<SecurityAuditLogResponse>> GetAsync(
        SecurityAuditQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page;
        var pageSize = query.PageSize == 0 ? 20 : query.PageSize;
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new IdentityValidationException("Page must be at least 1 and page size must be between 1 and 100.");
        }

        if (query.StartDateUtc.HasValue
            && query.EndDateUtc.HasValue
            && query.StartDateUtc.Value >= query.EndDateUtc.Value)
        {
            throw new IdentityValidationException("The start date must be earlier than the end date.");
        }

        var logs = _dbContext.SecurityAuditLogs.AsNoTracking();

        if (query.ActorUserId.HasValue)
        {
            logs = logs.Where(log => log.ActorUserId == query.ActorUserId);
        }

        if (query.TargetUserId.HasValue)
        {
            logs = logs.Where(log => log.TargetUserId == query.TargetUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            logs = logs.Where(log => log.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            logs = logs.Where(log => log.EntityType == entityType);
        }

        if (query.StartDateUtc.HasValue)
        {
            logs = logs.Where(log => log.OccurredAtUtc >= query.StartDateUtc.Value);
        }

        if (query.EndDateUtc.HasValue)
        {
            logs = logs.Where(log => log.OccurredAtUtc < query.EndDateUtc.Value);
        }

        var total = await logs.CountAsync(cancellationToken);
        var items = await logs
            .OrderByDescending(log => log.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new SecurityAuditLogResponse
            {
                Id = log.Id,
                ActorUserId = log.ActorUserId,
                TargetUserId = log.TargetUserId,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Outcome = log.Outcome,
                OccurredAtUtc = log.OccurredAtUtc,
                CorrelationId = log.CorrelationId,
                Details = log.Details
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SecurityAuditLogResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
