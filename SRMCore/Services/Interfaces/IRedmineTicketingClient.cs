using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IRedmineTicketingClient
{
    Task<RedmineTicketCreateResult> CreateIssueAsync(Incident incident, CancellationToken cancellationToken = default);
    Task AddCommentAsync(string externalTicketId, string comment, CancellationToken cancellationToken = default);
}
