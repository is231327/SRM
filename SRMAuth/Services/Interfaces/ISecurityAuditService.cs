namespace SRMAuth.Services.Interfaces;

public interface ISecurityAuditService
{
    Task RecordAsync(
        string eventType,
        string outcome,
        string actorIdentifier,
        string targetType = "",
        Guid? targetId = null,
        Guid? customerId = null,
        string description = "",
        CancellationToken cancellationToken = default);
}

public sealed class NullSecurityAuditService : ISecurityAuditService
{
    public Task RecordAsync(string eventType, string outcome, string actorIdentifier, string targetType = "", Guid? targetId = null, Guid? customerId = null, string description = "", CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
