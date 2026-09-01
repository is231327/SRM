namespace SRMAuth.Services;

public sealed class TooManyLoginAttemptsException(TimeSpan retryAfter)
    : Exception("Too many failed login attempts. Try again later.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
