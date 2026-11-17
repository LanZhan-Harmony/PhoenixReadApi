namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>统一负责红果 HTTP 调用的超时控制和响应正文读取。</summary>
internal sealed class PhoenixReadHttpTransport
{
    private readonly HttpClient _httpClient;
    private readonly PhoenixReadApiOptions _options;
    private long _serverClockOffsetMs;

    internal long ServerClockOffsetMs => Interlocked.Read(ref _serverClockOffsetMs);

    internal void UpdateServerClock(DateTimeOffset serverDate)
    {
        long offset =
            serverDate.ToUnixTimeMilliseconds() - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Interlocked.Exchange(ref _serverClockOffsetMs, offset);
    }

    internal async Task EnsureClockSyncedAsync(CancellationToken cancellationToken)
    {
        if (ServerClockOffsetMs != 0)
        {
            return;
        }

        try
        {
            using HttpRequestMessage probe = new(
                HttpMethod.Get,
                "https://api5-normal-sinfonlinea.fqnovel.com/"
            );
            using HttpResponseMessage res = await SendAsync(probe, cancellationToken)
                .ConfigureAwait(false);
            if (res.Headers.Date is { } serverDate)
            {
                UpdateServerClock(serverDate);
            }
        }
        catch
        {
            // probe fallback
        }
    }

    internal PhoenixReadHttpTransport(HttpClient httpClient, PhoenixReadApiOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    internal async Task<(HttpResponseMessage Response, byte[] Body)> SendAndReadAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Action<HttpResponseMessage>? onHeadersReceived = null,
        int? maxBytes = null
    )
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        timeout.CancelAfter(_options.RequestTimeout);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 请求超时.",
                request.RequestUri?.AbsolutePath,
                request.RequestUri,
                isTransient: true,
                exception
            );
        }
        catch (HttpRequestException exception)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 网络请求失败.",
                request.RequestUri?.AbsolutePath,
                request.RequestUri,
                isTransient: true,
                exception
            );
        }

        if (response.Headers.Date is { } serverDate)
        {
            UpdateServerClock(serverDate);
        }

        onHeadersReceived?.Invoke(response);

        byte[] body;
        try
        {
            body = await ResponseReader
                .ReadBytesAsync(response, maxBytes ?? _options.MaxResponseBytes, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 响应正文读取超时.",
                response.RequestMessage?.RequestUri?.AbsolutePath,
                response.RequestMessage?.RequestUri,
                isTransient: true,
                exception
            );
        }

        return (response, body);
    }

    internal async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        timeout.CancelAfter(_options.RequestTimeout);
        try
        {
            HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (response.Headers.Date is { } serverDate)
            {
                UpdateServerClock(serverDate);
            }
            return response;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 请求超时.",
                request.RequestUri?.AbsolutePath,
                request.RequestUri,
                isTransient: true,
                exception
            );
        }
        catch (HttpRequestException exception)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 网络请求失败.",
                request.RequestUri?.AbsolutePath,
                request.RequestUri,
                isTransient: true,
                exception
            );
        }
    }

    internal async Task<byte[]> ReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        int? maxBytes = null
    )
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        timeout.CancelAfter(_options.RequestTimeout);
        try
        {
            return await ResponseReader
                .ReadBytesAsync(response, maxBytes ?? _options.MaxResponseBytes, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new PhoenixReadApiTransportException(
                "Phoenix 响应正文读取超时.",
                response.RequestMessage?.RequestUri?.AbsolutePath,
                response.RequestMessage?.RequestUri,
                isTransient: true,
                exception
            );
        }
    }
}
