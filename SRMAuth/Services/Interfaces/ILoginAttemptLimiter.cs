namespace SRMAuth.Services.Interfaces;

public interface ILoginAttemptLimiter
{
    Task EnsureAllowedAsync(string principalType, string identifier, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(string principalType, string identifier, CancellationToken cancellationToken = default);
    Task ResetAsync(string principalType, string identifier, CancellationToken cancellationToken = default);
}

public sealed class NullLoginAttemptLimiter : ILoginAttemptLimiter
{
    public Task EnsureAllowedAsync(string principalType, string identifier, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RecordFailureAsync(string principalType, string identifier, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ResetAsync(string principalType, string identifier, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
