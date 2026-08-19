namespace SRMAgent.Services.Interfaces;

public interface IAgentAuthApiClient
{
    Task<string?> LoginAsync(CancellationToken cancellationToken = default);
}
