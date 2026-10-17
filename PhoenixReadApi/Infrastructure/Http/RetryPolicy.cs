namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>App 和网页传输共用的有限次数请求尝试策略。</summary>
internal static class RetryPolicy
{
    internal static async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> operation,
        int attempts,
        Func<Exception, bool> isTransient,
        CancellationToken cancellationToken
    )
    {
        Exception? last = null;
        attempts = Math.Clamp(attempts, 1, 8);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await operation(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested && attempt + 1 < attempts)
            {
                last = new TimeoutException("Phoenix request timed out.");
            }
            catch (Exception exception) when (isTransient(exception) && attempt + 1 < attempts)
            {
                last = exception;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken)
                .ConfigureAwait(false);
        }

        throw last ?? new PhoenixReadApiTransportException("Phoenix request failed.");
    }

    internal static bool IsTransient(Exception exception) =>
        exception
            is HttpRequestException
                or TimeoutException
                or PhoenixReadApiTransportException { IsTransient: true }
                or PhoenixReadApiHttpException { IsTransient: true }
                or PhoenixReadApiProviderException { IsTransient: true };
}
