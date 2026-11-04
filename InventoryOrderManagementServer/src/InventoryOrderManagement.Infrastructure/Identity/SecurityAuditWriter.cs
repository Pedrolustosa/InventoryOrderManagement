using InventoryOrderManagement.Application.Abstractions.Identity;
using InventoryOrderManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InventoryOrderManagement.Infrastructure.Identity;

public sealed class SecurityAuditWriter : ISecurityAuditWriter
{
    private readonly InventoryDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SecurityAuditWriter> _logger;

    public SecurityAuditWriter(
        InventoryDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SecurityAuditWriter> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var details = entry.Details;
            if (details is { Length: > 500 })
            {
                details = details[..500];
            }

            _dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = entry.ActorUserId,
                TargetUserId = entry.TargetUserId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Outcome = entry.Outcome,
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = _httpContextAccessor.HttpContext?.TraceIdentifier,
                Details = details
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Security audit entry {Action} could not be stored.", entry.Action);
        }
    }
}
