using SRMAgent.Models.Shelly;

namespace SRMAgent.Services.Interfaces;

public interface IVirtualShellyClient
{
    Task<VirtualShellyStatusResponse?> GetStatusAsync(string baseUrl, CancellationToken cancellationToken = default);
}
