using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IIncidentQueryService
{
    Task<List<Incident>> GetAllAsync(bool includeClosed = false, CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
