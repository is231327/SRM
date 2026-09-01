using SRMAuth.Data;
using SRMAuth.Security;
using SRMAuth.Services.Interfaces;
using SRMShared.Entities;

namespace SRMAuth.Services;

public sealed class SecurityAuditService(
    SrmAuthDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IHttpContextAccessor httpContextAccessor) : ISecurityAuditService
{
    public async Task RecordAsync(
        string eventType,
        string outcome,
        string actorIdentifier,
        string targetType = "",
        Guid? targetId = null,
        Guid? customerId = null,
        string description = "",
        CancellationToken cancellationToken = default)
    {
        dbContext.SecurityAuditRecords.Add(new SecurityAuditRecord
        {
            EventType = eventType,
            Outcome = outcome,
            ActorId = currentUserContext.UserId,
            ActorIdentifier = Limit(actorIdentifier, 200),
            SourceAddress = Limit(httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(), 128),
            TargetType = Limit(targetType, 100),
            TargetId = targetId,
            CustomerId = customerId,
            Description = Limit(description, 1000)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Limit(string? value, int maximumLength)
        => string.IsNullOrEmpty(value) ? string.Empty : value[..Math.Min(value.Length, maximumLength)];
}
