using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IIncidentQueryService
{
    Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
