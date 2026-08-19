namespace SRMAgent.Services.Interfaces;

public interface IRetryExecutor
{
    Task<T?> ExecuteAsync<T>(
        Func<CancellationToken, Task<T?>> operation,
        int maxAttempts,
        TimeSpan initialDelay,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> ExecuteAsync(
        Func<CancellationToken, Task<bool>> operation,
        int maxAttempts,
        TimeSpan initialDelay,
        CancellationToken cancellationToken = default);
}
