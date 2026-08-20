using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface ITicketDispatchService
{
    Task QueueCreateAsync(Incident incident, CancellationToken cancellationToken = default);
    Task QueueResolutionCommentAsync(Incident incident, string comment, CancellationToken cancellationToken = default);
}
