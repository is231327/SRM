using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IRedmineTicketingClient
{
    Task<RedmineTicketCreateResult> CreateIssueAsync(Incident incident, CancellationToken cancellationToken = default);
    Task AddCommentAsync(string externalTicketId, string comment, CancellationToken cancellationToken = default);
    Task UpdatePriorityAsync(string externalTicketId, IncidentSeverity severity, CancellationToken cancellationToken = default);
    Task<RedmineIssueDetails> GetIssueAsync(string externalTicketId, CancellationToken cancellationToken = default);
}
