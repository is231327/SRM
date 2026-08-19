using SRMAgent.Services.Interfaces;

namespace SRMAgent.Services;

public class RetryExecutor(ILogger<RetryExecutor> logger) : IRetryExecutor
{
    public async Task<T?> ExecuteAsync<T>(
        Func<CancellationToken, Task<T?>> operation,
        int maxAttempts,
        TimeSpan initialDelay,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(operation);

        var delay = initialDelay;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken);
                if (result is not null)
                {
                    return result;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Retryable operation failed on attempt {Attempt}.", attempt);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        return null;
    }

    public async Task<bool> ExecuteAsync(
        Func<CancellationToken, Task<bool>> operation,
        int maxAttempts,
        TimeSpan initialDelay,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var delay = initialDelay;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await operation(cancellationToken))
                {
                    return true;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Retryable operation failed on attempt {Attempt}.", attempt);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        return false;
    }
}
