using Polly;
using Polly.Timeout;
using Polly.Retry;
using Polly.Wrap;
using Microsoft.Extensions.Logging;


namespace Infrastructure;

public static class PollyPolicyFactory
{
    public static AsyncPolicyWrap CreateDefaultPolicy(ILogger? logger = null)
    {
        // Retry with jitter
        var retry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(new Random().Next(0,150)),
                onRetry: (ex, ts, attempt, ctx) =>
                {
                    logger?.LogWarning(ex, "Retry {Attempt} after {Delay} due to {Message}", attempt, ts, ex.Message);
                });

        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(15), TimeoutStrategy.Pessimistic);

        return Policy.WrapAsync(retry, timeout);
    }
}
