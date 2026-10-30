using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Web.Auth;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>构建并签名红果 App 请求，然后反序列化响应。</summary>
/// <remarks>读取请求按配置重试；短信、登录、评论和其他有副作用的写请求由调用方指定不重试。</remarks>
internal sealed class PhoenixReadAppTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private readonly PhoenixReadSessionStore _sessionStore;
    private readonly PhoenixReadDeviceManager _deviceManager;
    private readonly PhoenixReadPassportTransport _passportTransport;

    internal PhoenixReadAppTransport(PhoenixReadHttpTransport http, PhoenixReadApiOptions options)
    {
        _http = http;
        _options = options;
        _sessionStore = new PhoenixReadSessionStore(options);
        _deviceManager = new PhoenixReadDeviceManager(http, options.UserAgent, _sessionStore);
        _passportTransport = new PhoenixReadPassportTransport(
            http,
            options,
            _deviceManager,
            _sessionStore
        );
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
        RetryPolicy.ExecuteAsync(
            (attempt, token) => SendGetOnceAsync<TResponse>(path, query, token),
            _options.RetryCount,
            RetryPolicy.IsTransient,
            cancellationToken
        );

    /// <summary>读取顶层 video_data/local_book_data 的播放进度接口。</summary>
    internal Task<AppReadProgressResponse> GetReadProgressAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    ) =>
        RetryPolicy.ExecuteAsync(
            (_, token) => SendReadProgressOnceAsync(path, query, token),
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
        RetryPolicy.ExecuteAsync(
            (_, token) => SendSearchTabOnceAsync(path, query, token),
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

    internal Task<WebPassportResponse<TResponse>> GetWebPassportJsonAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    ) => _passportTransport.GetWebJsonAsync<TResponse>(path, query, cancellationToken);

    internal Task FollowRedirectAsync(string redirectUrl, CancellationToken cancellationToken) =>
        _passportTransport.FollowRedirectAsync(redirectUrl, cancellationToken);

    internal bool HasSession => _sessionStore.HasSession;

    internal AuthenticatedUser RestoredUser => _sessionStore.RestoredUser;

    internal void SaveAuthenticatedUser(AuthenticatedUser user) =>
        _sessionStore.SaveAuthenticatedUser(user);

    internal void SaveAuthenticationMetadata(
        string verificationTicket,
        string maskedPhone,
        int retryAfterSeconds
    ) =>
        _sessionStore.SaveAuthenticationMetadata(
            verificationTicket,
            maskedPhone,
            retryAfterSeconds
        );

    internal void ClearSession() => _sessionStore.ClearSession();

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
            await _deviceManager
                .EnsureRegisteredAsync(forceNew: false, cancellationToken)
                .ConfigureAwait(false);
        }

        int rotations = 0;
        while (true)
        {
            Uri endpoint = BuildAppUri(path);
            foreach (KeyValuePair<string, string> pair in query)
            {
                endpoint = HttpUriHelper.AddQuery(endpoint, pair.Key, pair.Value);
            }

            Uri requestUri = HttpUriHelper.AddQuery(
                endpoint,
                "_rticket",
                DateTimeOffset
                    .UtcNow.ToUnixTimeMilliseconds()
                    .ToString(CultureInfo.InvariantCulture)
            );
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
            request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
            _sessionStore.ApplySession(request);

            if (!isBookmallTab)
            {
                PhoenixReadRequestSigner.SignPlayer(
                    request,
                    [],
                    _deviceManager.DeviceId,
                    DateTimeOffset.UtcNow
                );
            }

            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            _sessionStore.CaptureSession(response);
            byte[] bytes = await _http
                .ReadBodyAsync(response, cancellationToken)
                .ConfigureAwait(false);

            if (
                !isBookmallTab
                && (bytes.Length == 0 || (int)response.StatusCode == 403)
                && rotations < 2
            )
            {
                rotations++;
                await _deviceManager
                    .EnsureRegisteredAsync(forceNew: true, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                bool transient = (int)response.StatusCode >= 500;
                throw new PhoenixReadApiHttpException(
                    $"红果 App 接口 HTTP {(int)response.StatusCode}。",
                    path,
                    (int)response.StatusCode,
                    requestUri,
                    transient
                );
            }

            if (bytes.Length == 0)
            {
                throw new PhoenixReadApiResponseException("红果 App 接口未返回有效数据。", path);
            }

            AppResponse<TResponse> result;
            try
            {
                result =
                    JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
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

    private async Task<AppReadProgressResponse> SendReadProgressOnceAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        await _deviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        Uri endpoint = BuildAppUri(path);
        foreach (KeyValuePair<string, string> pair in query)
        {
            endpoint = HttpUriHelper.AddQuery(endpoint, pair.Key, pair.Value);
        }

        Uri requestUri = HttpUriHelper.AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        _sessionStore.ApplySession(request);
        PhoenixReadRequestSigner.SignPlayer(
            request,
            [],
            _deviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        _sessionStore.CaptureSession(response);
        byte[] bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new PhoenixReadApiHttpException(
                $"红果 App 接口 HTTP {(int)response.StatusCode}。",
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

        AppReadProgressResponse result;
        try
        {
            result =
                JsonAotSerializer.Deserialize<AppReadProgressResponse>(bytes)
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

    private async Task<AppSearchTabResponse> SendSearchTabOnceAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        await _deviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        Uri endpoint = BuildAppUri(path);
        foreach (KeyValuePair<string, string> pair in query)
        {
            endpoint = HttpUriHelper.AddQuery(endpoint, pair.Key, pair.Value);
        }

        Uri requestUri = HttpUriHelper.AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        _sessionStore.ApplySession(request);
        PhoenixReadRequestSigner.SignPlayer(
            request,
            [],
            _deviceManager.DeviceId,
            DateTimeOffset.UtcNow
        );

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        _sessionStore.CaptureSession(response);
        byte[] bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new PhoenixReadApiHttpException(
                $"红果 App 接口 HTTP {(int)response.StatusCode}。",
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
            result =
                JsonAotSerializer.Deserialize<AppSearchTabResponse>(bytes)
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
        bool isMultiVideo = path.Contains(
            "/novel/player/multi_video_model/",
            StringComparison.Ordinal
        );

        if (!commentRequest && !isPassport && !isBookmallTab)
        {
            await _deviceManager
                .EnsureRegisteredAsync(forceNew: false, cancellationToken)
                .ConfigureAwait(false);
        }

        int rotations = 0;
        while (true)
        {
            Uri endpoint = BuildAppUri(path);
            Uri requestUri = HttpUriHelper.AddQuery(
                endpoint,
                "_rticket",
                DateTimeOffset
                    .UtcNow.ToUnixTimeMilliseconds()
                    .ToString(CultureInfo.InvariantCulture)
            );
            using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
            request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
            _sessionStore.ApplySession(request);
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

            DateTimeOffset now = DateTimeOffset.UtcNow;
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
            else if (isBookmallTab || isMultiVideo)
            {
                // MultiVideo and Bookmall Tab do not require Gorgon/Argus signatures
            }
            else
            {
                PhoenixReadRequestSigner.SignPlayer(request, body, _deviceManager.DeviceId, now);
            }

            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            _sessionStore.CaptureSession(response);
            byte[] bytes = await _http
                .ReadBodyAsync(response, cancellationToken)
                .ConfigureAwait(false);

            if (
                !commentRequest
                && !isPassport
                && !isBookmallTab
                && (bytes.Length == 0 || (int)response.StatusCode == 403)
                && rotations < 2
            )
            {
                rotations++;
                await _deviceManager
                    .EnsureRegisteredAsync(forceNew: true, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                bool transient = (int)response.StatusCode >= 500;
                throw new PhoenixReadApiHttpException(
                    $"红果 App 接口 HTTP {(int)response.StatusCode}。",
                    path,
                    (int)response.StatusCode,
                    requestUri,
                    transient
                );
            }

            if (bytes.Length == 0)
            {
                throw new PhoenixReadApiResponseException("红果 App 接口未返回有效数据。", path);
            }

            AppResponse<TResponse> result;
            try
            {
                result =
                    JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
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

    private Uri BuildAppUri(string path)
    {
        KeyValuePair<string, string>[] query = _deviceManager.BuildAppQuery();
        Uri baseUri = new(_options.AppBaseUri, path.TrimStart('/'));
        string queryText = string.Join(
            '&',
            query.Select(static pair =>
                Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)
            )
        );
        return new UriBuilder(baseUri) { Query = queryText }.Uri;
    }

    private static void EnsureSuccess(
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
        throw new PhoenixReadApiProviderException(
            $"红果 App 接口暂不可用（{detail}）。",
            path,
            providerCode,
            isTransient: true
        );
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
