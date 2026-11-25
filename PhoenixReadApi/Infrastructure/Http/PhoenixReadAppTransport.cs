using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Web.Authentication;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>构建并签名红果 App 请求，然后反序列化响应。</summary>
/// <remarks>读取请求按配置重试；短信、登录、评论和其他有副作用的写请求由调用方指定不重试。</remarks>
internal sealed class PhoenixReadAppTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private readonly PhoenixReadPassportTransport _passportTransport;
    private long _deviceCooldownUntilUnixMilliseconds;

    internal PhoenixReadDeviceManager DeviceManager { get; }

    internal PhoenixReadSessionStore SessionStore { get; }

    internal PhoenixReadAppTransport(PhoenixReadHttpTransport http, PhoenixReadApiOptions options)
    {
        _http = http;
        _options = options;
        SessionStore = new PhoenixReadSessionStore(options);
        DeviceManager = new PhoenixReadDeviceManager(http, options.UserAgent, SessionStore);
        _passportTransport = new PhoenixReadPassportTransport(
            http,
            options,
            DeviceManager,
            SessionStore
        );
    }

    /// <summary>探查或内部读取红果 App 接口的原始 UTF-8 字符串。</summary>
    internal async Task<string> GetRawStringAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        await DeviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);
        await AwaitDeviceCooldownAsync(cancellationToken).ConfigureAwait(false);
        Uri requestUri = BuildAppRequestUri(path, query);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        SessionStore.ApplySession(request);
        PhoenixReadRequestSigner.SignPlayer(
            request,
            [],
            DeviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );
        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
            .ConfigureAwait(false);
        response.Dispose();
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>探查或内部 POST 请求红果 App 接口的原始 UTF-8 字符串。</summary>
    internal async Task<string> PostRawStringAsync(
        string path,
        byte[] body,
        CancellationToken cancellationToken,
        int commentSource = 601,
        int? serverChannel = 1000
    )
    {
        await DeviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);
        await AwaitDeviceCooldownAsync(cancellationToken).ConfigureAwait(false);
        Uri requestUri = BuildAppRequestUri(path, []);
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        request.Headers.TryAddWithoutValidation(
            "Comment-Source",
            commentSource.ToString(CultureInfo.InvariantCulture)
        );
        if (serverChannel.HasValue)
        {
            request.Headers.TryAddWithoutValidation(
                "Server-Channel",
                serverChannel.Value.ToString(CultureInfo.InvariantCulture)
            );
        }
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8",
        };
        SessionStore.ApplySession(request);
        PhoenixReadRequestSigner.SignPlayer(
            request,
            body,
            DeviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );
        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
            .ConfigureAwait(false);
        response.Dispose();
        return Encoding.UTF8.GetString(bytes);
    }

    internal Task<AppResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        bool commentRequest,
        CancellationToken cancellationToken,
        int commentSource = 601,
        int? serverChannel = 1000,
        AppRequestRetryMode retryMode = AppRequestRetryMode.ReadOnly
    )
    {
        byte[] body = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        int attempts = retryMode == AppRequestRetryMode.NoRetry ? 1 : _options.RetryCount;
        if (attempts <= 1)
        {
            return SendOnceAsync<TResponse>(
                path,
                body,
                commentRequest,
                commentSource,
                serverChannel,
                cancellationToken
            );
        }

        return RetryPolicy.ExecuteAsync(
            (attempt, token) =>
                SendOnceAsync<TResponse>(
                    path,
                    body,
                    commentRequest,
                    commentSource,
                    serverChannel,
                    token
                ),
            attempts,
            RetryPolicy.IsTransient,
            cancellationToken
        );
    }

    internal Task<AppResponse<TResponse>> GetAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    ) =>
        _options.RetryCount <= 1
            ? SendGetOnceAsync<TResponse>(path, query, cancellationToken)
            : RetryPolicy.ExecuteAsync(
                (attempt, token) => SendGetOnceAsync<TResponse>(path, query, token),
                _options.RetryCount,
                RetryPolicy.IsTransient,
                cancellationToken
            );

    /// <summary>读取顶层 search_tabs 的短剧搜索 Tab 接口。</summary>
    internal Task<AppSearchTabResponse> GetSearchTabAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    ) =>
        _options.RetryCount <= 1
            ? SendSearchTabOnceAsync(path, query, cancellationToken)
            : RetryPolicy.ExecuteAsync(
                (_, token) => SendSearchTabOnceAsync(path, query, token),
                _options.RetryCount,
                RetryPolicy.IsTransient,
                cancellationToken
            );

    /// <summary>读取未遵循通用 data 包装的顶层 JSON 接口。</summary>
    internal Task<TResponse> GetRootAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken,
        bool skipDeviceAndSignature = false
    ) =>
        _options.RetryCount <= 1
            ? SendRootOnceAsync<TResponse>(path, query, cancellationToken, skipDeviceAndSignature)
            : RetryPolicy.ExecuteAsync(
                (_, token) =>
                    SendRootOnceAsync<TResponse>(path, query, token, skipDeviceAndSignature),
                _options.RetryCount,
                RetryPolicy.IsTransient,
                cancellationToken
            );

    /// <summary>发送登录等接口使用的 application/x-www-form-urlencoded 请求。</summary>
    internal Task<AppResponse<TResponse>> PostFormAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> values,
        CancellationToken cancellationToken
    ) => _passportTransport.PostFormAsync<TResponse>(path, values, cancellationToken);

    /// <summary>发送 multipart/form-data 文件上传请求。</summary>
    internal Task<AppResponse<TResponse>> PostMultipartAsync<TResponse>(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken
    ) =>
        RetryPolicy.ExecuteAsync(
            (_, token) => SendMultipartOnceAsync<TResponse>(path, content, token),
            1,
            RetryPolicy.IsTransient,
            cancellationToken
        );

    internal Task<WebPassportResponse<TResponse>> GetWebPassportJsonAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    ) => _passportTransport.GetWebJsonAsync<TResponse>(path, query, cancellationToken);

    internal Task FollowRedirectAsync(string redirectUrl, CancellationToken cancellationToken) =>
        _passportTransport.FollowRedirectAsync(redirectUrl, cancellationToken);

    internal bool HasSession => SessionStore.HasSession;

    internal AuthenticatedUser RestoredUser => SessionStore.RestoredUser;

    internal void SaveAuthenticatedUser(AuthenticatedUser user) =>
        SessionStore.SaveAuthenticatedUser(user);

    internal void SaveAuthenticationMetadata(
        string verificationTicket,
        string maskedPhone,
        int retryAfterSeconds
    ) =>
        SessionStore.SaveAuthenticationMetadata(verificationTicket, maskedPhone, retryAfterSeconds);

    internal void ClearSession() => SessionStore.ClearSession();

    internal bool IsAuthenticated => SessionStore.HasSession;

    internal Task ResetDeviceAsync(CancellationToken cancellationToken) =>
        DeviceManager.ResetDeviceAsync(cancellationToken);

    private async Task<AppResponse<TResponse>> SendGetOnceAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        bool isBookmallTab = path.Contains(
            "/reading/bookapi/bookmall/tab/",
            StringComparison.Ordinal
        );
        if (!isBookmallTab)
        {
            await DeviceManager
                .EnsureRegisteredAsync(forceNew: false, cancellationToken)
                .ConfigureAwait(false);
        }

        await AwaitDeviceCooldownAsync(cancellationToken).ConfigureAwait(false);

        int rotations = 0;
        while (true)
        {
            Uri requestUri = BuildAppRequestUri(path, query);
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
            request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
            SessionStore.ApplySession(request);

            if (!isBookmallTab)
            {
                PhoenixReadRequestSigner.SignPlayer(
                    request,
                    [],
                    DeviceManager.DeviceId,
                    DateTimeOffset.UtcNow
                );
            }

            (HttpResponseMessage response, byte[] bytes) = await _http
                .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
                .ConfigureAwait(false);
            using (response)
            {
                if (
                    !isBookmallTab
                    && (bytes.Length == 0 || (int)response.StatusCode == 403)
                    && rotations < 2
                )
                {
                    rotations++;
                    await DeviceManager
                        .EnsureRegisteredAsync(forceNew: true, cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    bool transient = (int)response.StatusCode >= 500;
                    throw new PhoenixReadApiHttpException(
                        ExtractSafeHttpErrorMessage(bytes, (int)response.StatusCode),
                        path,
                        (int)response.StatusCode,
                        requestUri,
                        transient
                    );
                }

                if (bytes.Length == 0)
                {
                    throw new PhoenixReadApiResponseException(
                        "红果 App 接口未返回有效数据。",
                        path
                    );
                }

                AppResponse<TResponse> result;
                try
                {
                    result = JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
                        ?? throw new JsonException("The App response was null.");
                }
                catch (JsonException exception)
                {
                    throw new PhoenixReadApiDeserializationException(
                        "红果 App 接口返回格式异常。",
                        path,
                        exception
                    );
                }

                EnsureSuccess(result.Code, result.Message, result.BaseResponse, path);
                return result;
            }
        }
    }

    private async Task<AppSearchTabResponse> SendSearchTabOnceAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        await DeviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        Uri requestUri = BuildAppRequestUri(path, query);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        SessionStore.ApplySession(request);
        PhoenixReadRequestSigner.SignPlayer(
            request,
            [],
            DeviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );

        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
            .ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new PhoenixReadApiHttpException(
                    ExtractSafeHttpErrorMessage(bytes, (int)response.StatusCode),
                    path,
                    (int)response.StatusCode,
                    requestUri,
                    (int)response.StatusCode >= 500
                );
            }
            if (bytes.Length == 0)
            {
                throw new PhoenixReadApiResponseException("红果 App 接口未返回有效数据。", path);
            }

            AppSearchTabResponse result;
            try
            {
                result = JsonAotSerializer.Deserialize<AppSearchTabResponse>(bytes)
                    ?? throw new JsonException("The App response was null.");
            }
            catch (JsonException exception)
            {
                throw new PhoenixReadApiDeserializationException(
                    "红果 App 接口返回格式异常。",
                    path,
                    exception
                );
            }

            EnsureSuccess(result.Code, result.Message, result.BaseResponse, path);
            return result;
        }
    }

    private async Task<TResponse> SendRootOnceAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken,
        bool skipDeviceAndSignature = false
    )
    {
        if (!skipDeviceAndSignature)
        {
            await DeviceManager
                .EnsureRegisteredAsync(forceNew: false, cancellationToken)
                .ConfigureAwait(false);
        }

        Uri requestUri = BuildAppRequestUri(path, query);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        SessionStore.ApplySession(request);
        if (!skipDeviceAndSignature)
        {
            PhoenixReadRequestSigner.SignPlayer(
                request,
                [],
                DeviceManager.DeviceId,
                DateTimeOffset.UtcNow
            );
        }

        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
            .ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new PhoenixReadApiHttpException(
                    ExtractSafeHttpErrorMessage(bytes, (int)response.StatusCode),
                    path,
                    (int)response.StatusCode,
                    requestUri,
                    (int)response.StatusCode >= 500
                );
            }
            if (bytes.Length == 0)
            {
                throw new PhoenixReadApiResponseException("红果 App 接口未返回有效数据。", path);
            }

            try
            {
                return JsonAotSerializer.Deserialize<TResponse>(bytes)
                    ?? throw new JsonException("The App response was null.");
            }
            catch (JsonException exception)
            {
                throw new PhoenixReadApiDeserializationException(
                    "红果 App 接口返回格式异常。",
                    path,
                    exception
                );
            }
        }
    }

    private async Task<AppResponse<TResponse>> SendMultipartOnceAsync<TResponse>(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken
    )
    {
        await DeviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        await _http.EnsureClockSyncedAsync(cancellationToken).ConfigureAwait(false);

        await AwaitDeviceCooldownAsync(cancellationToken).ConfigureAwait(false);

        Uri requestUri = BuildAppRequestUri(path);
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        SessionStore.ApplySession(request);

        request.Content = content;
        PhoenixReadRequestSigner.SignPlayer(
            request,
            [],
            DeviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );

        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
            .ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new PhoenixReadApiHttpException(
                    ExtractSafeHttpErrorMessage(bytes, (int)response.StatusCode),
                    path,
                    (int)response.StatusCode,
                    requestUri,
                    (int)response.StatusCode >= 500
                );
            }

            if (bytes.Length == 0)
            {
                throw new PhoenixReadApiResponseException("红果 App 接口未返回有效数据。", path);
            }

            AppResponse<TResponse> result;
            try
            {
                result = JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
                    ?? throw new JsonException("The App response was null.");
            }
            catch (JsonException exception)
            {
                throw new PhoenixReadApiDeserializationException(
                    "红果 App 接口返回格式异常。",
                    path,
                    exception
                );
            }

            EnsureSuccess(result.Code, result.Message, result.BaseResponse, path);
            return result;
        }
    }

    private async Task<AppResponse<TResponse>> SendOnceAsync<TResponse>(
        string path,
        byte[] body,
        bool commentRequest,
        int commentSource,
        int? serverChannel,
        CancellationToken cancellationToken
    )
    {
        bool isBookmallTab = path.Contains(
            "/reading/bookapi/bookmall/tab/",
            StringComparison.Ordinal
        );
        bool isPassport =
            path.Contains("/passport/", StringComparison.Ordinal)
            || path.Contains("/novel/articleapi/do_action", StringComparison.Ordinal)
            || path.Contains("/reading/bookapi/read_history/update", StringComparison.Ordinal);

        if (!commentRequest && !isPassport && !isBookmallTab)
        {
            await DeviceManager
                .EnsureRegisteredAsync(forceNew: false, cancellationToken)
                .ConfigureAwait(false);
        }

        await AwaitDeviceCooldownAsync(cancellationToken).ConfigureAwait(false);

        int rotations = 0;
        while (true)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long nowMs = now.ToUnixTimeMilliseconds();

            Uri requestUri = BuildAppRequestUri(path);

            using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
            request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
            SessionStore.ApplySession(request);
            if (commentRequest)
            {
                request.Headers.TryAddWithoutValidation("lc", "101");
                request.Headers.TryAddWithoutValidation(
                    "Comment-Source",
                    commentSource.ToString(CultureInfo.InvariantCulture)
                );
                if (serverChannel is { } channel)
                {
                    request.Headers.TryAddWithoutValidation(
                        "Server-Channel",
                        channel.ToString(CultureInfo.InvariantCulture)
                    );
                }
            }

            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8",
            };

            if (commentRequest)
            {
                PhoenixReadRequestSigner.SignComment(
                    request,
                    PhoenixReadRequestSigner.CreateCommentNonce(),
                    now
                );
            }
            else if (isPassport)
            {
                PhoenixReadRequestSigner.SignPassport(request, body, now);
            }
            else if (isBookmallTab)
            {
                // Bookmall Tab does not require Gorgon/Argus signatures
            }
            else
            {
                PhoenixReadRequestSigner.SignPlayer(request, body, DeviceManager.DeviceId, now);
            }

            (HttpResponseMessage response, byte[] bytes) = await _http
                .SendAndReadAsync(request, cancellationToken, SessionStore.CaptureSession)
                .ConfigureAwait(false);
            using (response)
            {
                if (
                    !commentRequest
                    && !isPassport
                    && !isBookmallTab
                    && (bytes.Length == 0 || (int)response.StatusCode == 403)
                    && rotations < 2
                )
                {
                    rotations++;
                    await DeviceManager
                        .EnsureRegisteredAsync(forceNew: true, cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    bool transient = (int)response.StatusCode >= 500;
                    throw new PhoenixReadApiHttpException(
                        ExtractSafeHttpErrorMessage(bytes, (int)response.StatusCode),
                        path,
                        (int)response.StatusCode,
                        requestUri,
                        transient
                    );
                }

                if (bytes.Length == 0)
                {
                    throw new PhoenixReadApiResponseException(
                        "红果 App 接口未返回有效数据。",
                        path
                    );
                }

                AppResponse<TResponse> result;
                try
                {
                    result = JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
                        ?? throw new JsonException("The App response was null.");
                }
                catch (JsonException exception)
                {
                    throw new PhoenixReadApiDeserializationException(
                        "红果 App 接口返回格式异常。",
                        path,
                        exception
                    );
                }

                EnsureSuccess(result.Code, result.Message, result.BaseResponse, path);
                return result;
            }
        }
    }

    private Uri BuildAppRequestUri(
        string path,
        IEnumerable<KeyValuePair<string, string>>? extraQuery = null
    )
    {
        StringBuilder sb = new(512);
        string baseUri = _options.AppBaseUri.AbsoluteUri;
        sb.Append(baseUri);
        if (!baseUri.EndsWith('/') && !path.StartsWith('/'))
        {
            sb.Append('/');
        }
        else if (baseUri.EndsWith('/') && path.StartsWith('/'))
        {
            sb.Append(path.AsSpan(1));
            goto AppendQuery;
        }
        sb.Append(path);

        AppendQuery:
        sb.Append('?');
        DeviceManager.AppendAppQuery(sb);

        if (extraQuery is not null)
        {
            foreach (KeyValuePair<string, string> pair in extraQuery)
            {
                sb.Append('&');
                sb.Append(Uri.EscapeDataString(pair.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(pair.Value));
            }
        }

        long rticket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        sb.Append("&_rticket=");
        sb.Append(rticket);

        return new Uri(sb.ToString(), UriKind.Absolute);
    }

    private void EnsureSuccess(
        string? code,
        string? message,
        AppBaseResponse? baseResponse,
        string path
    )
    {
        string? providerCode = code;
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            providerCode = baseResponse?.StatusCode;
        }

        if (string.IsNullOrWhiteSpace(providerCode) || providerCode == "0")
        {
            return;
        }

        string serverMessage = !string.IsNullOrWhiteSpace(message)
            ? message.Trim()
            : (baseResponse?.StatusMessage ?? string.Empty).Trim();
        string detail = string.IsNullOrWhiteSpace(serverMessage)
            ? Truncate(providerCode, 20)
            : $"{Truncate(providerCode, 20)}: {Truncate(serverMessage, 100)}";

        TimeSpan? retryAfter = TryParseBanCooldown(providerCode, serverMessage);
        if (retryAfter is { } banCooldown)
        {
            long until =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                + (long)banCooldown.TotalMilliseconds;
            Interlocked.Exchange(ref _deviceCooldownUntilUnixMilliseconds, until);
        }

        throw new PhoenixReadApiProviderException(
            $"红果 App 接口暂不可用（{detail}）。",
            path,
            providerCode,
            isTransient: true,
            retryAfter: retryAfter
        );
    }

    private static TimeSpan? TryParseBanCooldown(string? code, string message)
    {
        if (code != "100" || string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        // 优先读取 max_ban_time 确保冷却期完全过去，避免在 min_ban_time 之后依然处于封禁窗口中；没有时回退到 min_ban_time
        int banIdx = message.IndexOf("\"max_ban_time\"", StringComparison.OrdinalIgnoreCase);
        if (banIdx < 0)
        {
            banIdx = message.IndexOf("\"min_ban_time\"", StringComparison.OrdinalIgnoreCase);
        }

        if (banIdx < 0)
        {
            return null;
        }

        int colonIdx = message.IndexOf(':', banIdx);
        if (colonIdx < 0)
        {
            return null;
        }

        int start = colonIdx + 1;
        while (start < message.Length && char.IsWhiteSpace(message[start]))
        {
            start++;
        }

        int end = start;
        while (end < message.Length && char.IsDigit(message[end]))
        {
            end++;
        }

        if (
            end > start
            && int.TryParse(
                message.AsSpan(start, end - start),
                CultureInfo.InvariantCulture,
                out int seconds
            )
            && seconds > 0
        )
        {
            return TimeSpan.FromSeconds(Math.Min(seconds, 30));
        }

        return null;
    }

    private async Task AwaitDeviceCooldownAsync(CancellationToken cancellationToken)
    {
        long until = Interlocked.Read(ref _deviceCooldownUntilUnixMilliseconds);
        if (until <= 0)
        {
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long diff = until - now;
        if (diff is > 0 and <= 15_000)
        {
            int jitterMs = Random.Shared.Next(100, 300);
            await Task.Delay(TimeSpan.FromMilliseconds(diff + jitterMs), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private static string ExtractSafeHttpErrorMessage(byte[] bytes, int statusCode)
    {
        if (bytes.Length > 0)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(bytes);
                if (
                    doc.RootElement.TryGetProperty("message", out JsonElement msgProp)
                    || doc.RootElement.TryGetProperty("msg", out msgProp)
                    || doc.RootElement.TryGetProperty("status_msg", out msgProp)
                )
                {
                    string? msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg) && msg.Length <= 100)
                    {
                        return $"红果 App 接口 HTTP {statusCode} ({msg.Trim()})。";
                    }
                }
            }
            catch
            {
                // ignore non-json error responses
            }
        }
        return $"红果 App 接口 HTTP {statusCode}。";
    }
}
