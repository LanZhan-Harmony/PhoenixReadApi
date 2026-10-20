using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>构建并签名红果 App 请求，然后反序列化响应。</summary>
/// <remarks>读取请求按配置重试；短信、登录、评论和其他有副作用的写请求由调用方指定不重试。</remarks>
internal sealed class PhoenixReadAppTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private readonly ConcurrentDictionary<string, string> _cookies = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ConcurrentDictionary<string, string> _authHeaders = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly string? _sessionStateFilePath;
    private readonly object _sessionFileLock = new();
    private string _deviceId;
    private string _installId;
    private string _cdid = string.Empty;
    private string _klinkEgdi = "AAJJM_WQMzpUFSGpW0di3XOPy8QZX_ge-bxBdCmFsijoUL4RQJ0-xSCG";
    private string _passportCsrfToken = string.Empty;
    private string _ttReq = string.Empty;
    private string _userId = string.Empty;
    private string _userName = string.Empty;
    private string _userAvatarUrl = string.Empty;
    private string _userDescription = string.Empty;
    private string _userMaskedPhone = string.Empty;

    private const string AuthenticationVersionCode = "73932";
    private const string AuthenticationVersionName = "7.3.9.32";
    private const string AuthenticationChannel = "69310258";
    private const string AuthenticationDeviceType = "AOSP on Intel Platform";
    private const string AuthenticationDeviceBrand = "SAMSUNG";
    private const string AuthenticationResolution = "1920*1080";
    private const string AuthenticationRomVersion = "V417IR release-keys";

    internal PhoenixReadAppTransport(PhoenixReadHttpTransport http, PhoenixReadApiOptions options)
    {
        _http = http;
        _options = options;
        _sessionStateFilePath =
            options.AuthenticationStateFilePath ?? options.AnonymousStateFilePath;
        _deviceId = options.DeviceId!;
        _installId = options.InstallId!;
        LoadSessionSnapshot();
        if (string.IsNullOrWhiteSpace(_cdid))
        {
            _cdid = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(_ttReq))
        {
            Span<byte> value = stackalloc byte[16];
            RandomNumberGenerator.Fill(value);
            _ttReq = "1$" + Convert.ToHexString(value).ToLowerInvariant();
        }
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
        var body = JsonAotSerializer.SerializeToUtf8Bytes(payload);
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

    /// <summary>发送返回顶层播放进度列表的接口。</summary>
    internal Task<AppReadProgressResponse> PostReadProgressAsync<TRequest>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken,
        AppRequestRetryMode retryMode = AppRequestRetryMode.ReadOnly
    )
    {
        byte[] body = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        int attempts = retryMode == AppRequestRetryMode.NoRetry ? 1 : _options.RetryCount;
        return RetryPolicy.ExecuteAsync(
            (_, token) => SendReadProgressPostOnceAsync(path, body, token),
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

    private async Task<AppResponse<TResponse>> SendGetOnceAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        Uri endpoint = BuildAppUri(path);
        foreach (KeyValuePair<string, string> pair in query)
        {
            endpoint = AddQuery(endpoint, pair.Key, pair.Value);
        }

        Uri requestUri = AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        ApplySession(request);
        PhoenixReadRequestSigner.Sign(request, ReadOnlySpan<byte>.Empty, DateTimeOffset.UtcNow);

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        CaptureSession(response);
        var bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var transient = (int)response.StatusCode >= 500;
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

        var providerCode = result.Code;
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            providerCode = result.BaseResponse?.StatusCode;
        }
        if (!string.IsNullOrWhiteSpace(providerCode) && providerCode != "0")
        {
            throw new PhoenixReadApiProviderException(
                $"红果 App 接口暂不可用（{Truncate(providerCode, 20)}）。",
                path,
                providerCode,
                isTransient: true
            );
        }

        return result;
    }

    private async Task<AppReadProgressResponse> SendReadProgressOnceAsync(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        Uri endpoint = BuildAppUri(path);
        foreach (KeyValuePair<string, string> pair in query)
        {
            endpoint = AddQuery(endpoint, pair.Key, pair.Value);
        }

        Uri requestUri = AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        ApplySession(request);
        PhoenixReadRequestSigner.Sign(request, ReadOnlySpan<byte>.Empty, DateTimeOffset.UtcNow);

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        CaptureSession(response);
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

        string providerCode = result.Code;
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            providerCode = result.BaseResponse?.StatusCode ?? string.Empty;
        }
        if (!string.IsNullOrWhiteSpace(providerCode) && providerCode != "0")
        {
            throw new PhoenixReadApiProviderException(
                $"红果 App 接口暂不可用（{Truncate(providerCode, 20)}）。",
                path,
                providerCode,
                isTransient: true
            );
        }
        return result;
    }

    private async Task<AppReadProgressResponse> SendReadProgressPostOnceAsync(
        string path,
        byte[] body,
        CancellationToken cancellationToken
    )
    {
        Uri endpoint = BuildAppUri(path);
        Uri requestUri = AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        ApplySession(request);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8",
        };
        PhoenixReadRequestSigner.Sign(request, body, DateTimeOffset.UtcNow);

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        CaptureSession(response);
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
        string providerCode = result.Code;
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            providerCode = result.BaseResponse?.StatusCode ?? string.Empty;
        }
        if (!string.IsNullOrWhiteSpace(providerCode) && providerCode != "0")
        {
            throw new PhoenixReadApiProviderException(
                $"红果 App 接口暂不可用（{Truncate(providerCode, 20)}）。",
                path,
                providerCode,
                isTransient: true
            );
        }
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
        Uri endpoint = BuildAppUri(path);
        Uri requestUri = AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        ApplySession(request);
        if (commentRequest)
        {
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
        else if (!path.Contains("/novel/player/video_model/", StringComparison.Ordinal))
        {
            if (
                path.Contains("/novel/articleapi/do_action", StringComparison.Ordinal)
                || path.Contains("/reading/bookapi/read_history/update", StringComparison.Ordinal)
            )
            {
                PhoenixReadRequestSigner.SignPassport(request, body, now);
            }
            else
            {
                PhoenixReadRequestSigner.Sign(request, body, now);
            }
        }

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        CaptureSession(response);
        var bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var transient = (int)response.StatusCode >= 500;
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

        var providerCode = result.Code;
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            providerCode = result.BaseResponse?.StatusCode;
        }
        if (!string.IsNullOrWhiteSpace(providerCode) && providerCode != "0")
        {
            throw new PhoenixReadApiProviderException(
                $"红果 App 接口暂不可用（{Truncate(providerCode, 20)}）。",
                path,
                providerCode,
                isTransient: true
            );
        }

        return result;
    }

    /// <summary>发送登录等接口使用的 application/x-www-form-urlencoded 请求。</summary>
    internal Task<AppResponse<TResponse>> PostFormAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> values,
        CancellationToken cancellationToken
    )
    {
        EnsureAuthenticationSession();
        KeyValuePair<string, string>[] items = BuildAuthenticationFormValues(path, values);
        var encoded = string.Join(
            '&',
            items.Select(static pair =>
                Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)
            )
        );
        byte[] body = System.Text.Encoding.UTF8.GetBytes(encoded);
        return RetryPolicy.ExecuteAsync(
            (_, token) => SendFormOnceAsync<TResponse>(path, body, token),
            1,
            RetryPolicy.IsTransient,
            cancellationToken
        );
    }

    internal bool HasSession =>
        _authHeaders.ContainsKey("x-tt-token")
        && (_cookies.ContainsKey("sessionid") || _cookies.ContainsKey("sid_tt"));

    internal AuthenticatedUser RestoredUser =>
        new()
        {
            Id = _userId,
            Name = _userName,
            AvatarUrl = _userAvatarUrl,
            Description = _userDescription,
            MaskedPhone = _userMaskedPhone,
        };

    internal void SaveAuthenticatedUser(AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _userId = user.Id;
        _userName = user.Name;
        _userAvatarUrl = user.AvatarUrl;
        _userDescription = user.Description;
        _userMaskedPhone = user.MaskedPhone;
        SaveSessionSnapshot();
    }

    internal void SaveAuthenticationMetadata(
        string verificationTicket,
        string maskedPhone,
        int retryAfterSeconds
    ) => SaveSessionSnapshot(verificationTicket, maskedPhone, retryAfterSeconds);

    internal void ClearSession()
    {
        _cookies.Clear();
        _authHeaders.Clear();
        _passportCsrfToken = string.Empty;
        _userId = string.Empty;
        _userName = string.Empty;
        _userAvatarUrl = string.Empty;
        _userDescription = string.Empty;
        _userMaskedPhone = string.Empty;
        DeleteSessionSnapshot();
    }

    private async Task<AppResponse<TResponse>> SendFormOnceAsync<TResponse>(
        string path,
        byte[] body,
        CancellationToken cancellationToken
    )
    {
        Uri endpoint = BuildAppUri(path, authenticationRequest: true);
        Uri requestUri = AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "com.phoenix.read/73932 (Linux; U; Android 15; zh_CN_#Hans; AOSP on Intel Platform; Build/SQ1A.220205.002;tt-ok/3.12.13.20)"
        );
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        request.Headers.TryAddWithoutValidation("Passport-Sdk-Version", "5051452");
        request.Headers.TryAddWithoutValidation("X-VC-Bdturing-Sdk-Version", "4.0.3.cn");
        request.Headers.TryAddWithoutValidation("lc", "101");
        ApplySession(request);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(
            "application/x-www-form-urlencoded"
        )
        {
            CharSet = "utf-8",
        };
        PhoenixReadRequestSigner.SignPassport(request, body, DateTimeOffset.UtcNow);
        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        CaptureSession(response);
        var bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            SaveAuthenticationResponseDiagnostic(path, response, bytes);
            if (
                (int)response.StatusCode == 403
                && path.Contains("/passport/", StringComparison.Ordinal)
            )
            {
                throw new PhoenixReadApiAuthenticationException(
                    "登录服务拒绝了当前设备请求，未发送或未接受验证码。",
                    path,
                    "AUTH_REQUIRED"
                );
            }
            throw new PhoenixReadApiHttpException(
                $"红果 App 接口 HTTP {(int)response.StatusCode}。",
                path,
                (int)response.StatusCode,
                requestUri,
                (int)response.StatusCode >= 500
            );
        }

        try
        {
            AppResponse<TResponse> result =
                JsonAotSerializer.Deserialize<AppResponse<TResponse>>(bytes)
                ?? throw new JsonException("The App response was null.");
            string providerCode = result.Code;
            if (string.IsNullOrWhiteSpace(providerCode))
            {
                providerCode = result.BaseResponse?.StatusCode ?? string.Empty;
            }
            if (!string.IsNullOrWhiteSpace(providerCode) && providerCode != "0")
            {
                throw new PhoenixReadApiProviderException(
                    $"红果 App 接口暂不可用（{Truncate(providerCode, 20)}）。",
                    path,
                    providerCode,
                    isTransient: true
                );
            }
            return result;
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

    private Uri BuildAppUri(string path) => BuildAppUri(path, authenticationRequest: false);

    private Uri BuildAppUri(string path, bool authenticationRequest)
    {
        if (authenticationRequest)
        {
            long ticket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            KeyValuePair<string, string>[] authenticationQuery =
            [
                new("passport-sdk-version", "5051452"),
                new("use_new_token_expire_rule", "true"),
                new("klink_egdi", _klinkEgdi),
                new("iid", _installId),
                new("device_id", _deviceId),
                new("ac", "wifi"),
                new("channel", AuthenticationChannel),
                new("aid", "8662"),
                new("app_name", "novelread"),
                new("version_code", AuthenticationVersionCode),
                new("version_name", AuthenticationVersionName),
                new("device_platform", "android"),
                new("os", "android"),
                new("ssmix", "a"),
                new("device_type", AuthenticationDeviceType),
                new("device_brand", AuthenticationDeviceBrand),
                new("language", "zh"),
                new("os_api", "35"),
                new("os_version", "15"),
                new("manifest_version_code", AuthenticationVersionCode),
                new("resolution", AuthenticationResolution),
                new("dpi", "280"),
                new("update_version_code", AuthenticationVersionCode),
                new("_rticket", ticket.ToString(CultureInfo.InvariantCulture)),
                new("host_abi", "arm64-v8a"),
                new("dragon_device_type", "pad"),
                new("pv_player", AuthenticationVersionCode),
                new("compliance_status", "0"),
                new("need_personal_recommend", "1"),
                new("player_so_load", "1"),
                new("is_android_pad_screen", "1"),
                new("rom_version", AuthenticationRomVersion),
                new("cdid", _cdid),
                new("okhttp_version", "4.2.243.31-douyin"),
                new("use_store_region_cookie", "1"),
            ];
            return CreateRawUri(_options.AuthenticationBaseUri, path, authenticationQuery);
        }

        KeyValuePair<string, string>[] query =
        [
            new KeyValuePair<string, string>("aid", "8662"),
            new("app_name", "novelread"),
            new("version_code", AuthenticationVersionCode),
            new("version_name", AuthenticationVersionName),
            new("manifest_version_code", AuthenticationVersionCode),
            new("update_version_code", AuthenticationVersionCode),
            new("channel", AuthenticationChannel),
            new("device_platform", "android"),
            new("os", "android"),
            new("ssmix", "a"),
            new("device_type", AuthenticationDeviceType),
            new("device_brand", AuthenticationDeviceBrand),
            new("language", "zh"),
            new("os_api", "35"),
            new("os_version", "15"),
            new("resolution", AuthenticationResolution),
            new("dpi", "280"),
            new("ac", "wifi"),
            new("device_id", _deviceId),
            new("iid", _installId),
            new("host_abi", "arm64-v8a"),
            new("dragon_device_type", "pad"),
            new("pv_player", AuthenticationVersionCode),
            new("compliance_status", "0"),
            new("need_personal_recommend", "1"),
            new("player_so_load", "1"),
            new("is_android_pad_screen", "1"),
            new("rom_version", AuthenticationRomVersion),
            new("cdid", _cdid),
        ];
        Uri baseUri = new(
            authenticationRequest ? _options.AuthenticationBaseUri : _options.AppBaseUri,
            path.TrimStart('/')
        );
        var queryText = string.Join(
            '&',
            query.Select(static pair =>
                Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)
            )
        );
        return new UriBuilder(baseUri) { Query = queryText }.Uri;
    }

    private static Uri CreateRawUri(
        Uri baseUri,
        string path,
        IEnumerable<KeyValuePair<string, string>> query
    )
    {
        string queryText = string.Join(
            '&',
            query.Select(static pair =>
                EncodeQueryComponent(pair.Key) + "=" + EncodeQueryComponent(pair.Value)
            )
        );
        return new Uri(
            baseUri.AbsoluteUri.TrimEnd('/') + "/" + path.TrimStart('/') + "?" + queryText,
            UriKind.Absolute
        );

        static string EncodeQueryComponent(string value) =>
            Uri.EscapeDataString(value).Replace("%2A", "*", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri AddQuery(Uri uri, string key, string value)
    {
        var query = uri.Query.TrimStart('?');
        var joined = query.Length == 0 ? string.Empty : query + "&";
        joined += Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
        return new UriBuilder(uri) { Query = joined }.Uri;
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private void ApplySession(HttpRequestMessage request)
    {
        if (_cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", _cookies.Select(static pair => $"{pair.Key}={pair.Value}"))
            );
        }

        foreach (KeyValuePair<string, string> header in _authHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private void CaptureSession(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
        {
            foreach (string value in values)
            {
                string[] parts = value.Split(';', 2, StringSplitOptions.TrimEntries);
                string[] pair = parts[0].Split('=', 2);
                if (pair.Length == 2 && pair[0].Length > 0)
                {
                    if (pair[1].Length == 0)
                    {
                        _cookies.TryRemove(pair[0], out _);
                    }
                    else
                    {
                        _cookies[pair[0]] = pair[1];
                    }
                }
            }
        }

        foreach (
            string name in new[]
            {
                "x-tt-token",
                "x-tt-token-sign",
                "x-tt-token-supplement",
                "x-tt-passport-mfa-token",
                "x-tt-passport-csrf-token",
                "d-ticket",
            }
        )
        {
            if (response.Headers.TryGetValues(name, out IEnumerable<string>? headerValues))
            {
                string? value = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _authHeaders[name] = value;
                }
            }
        }
        SaveSessionSnapshot();
    }

    private KeyValuePair<string, string>[] BuildAuthenticationFormValues(
        string path,
        IEnumerable<KeyValuePair<string, string>> values
    )
    {
        List<KeyValuePair<string, string>> result = [.. values];
        long ticket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Add("klink_egdi", _klinkEgdi);
        Add("iid", _installId);
        Add("device_id", _deviceId);
        Add("ac", "wifi");
        Add("channel", AuthenticationChannel);
        Add("aid", "8662");
        Add("app_name", "novelread");
        Add("version_code", AuthenticationVersionCode);
        Add("version_name", AuthenticationVersionName);
        Add("device_platform", "android");
        Add("os", "android");
        Add("ssmix", "a");
        Add("device_type", AuthenticationDeviceType);
        Add("device_brand", AuthenticationDeviceBrand);
        Add("language", "zh");
        Add("os_api", "35");
        Add("os_version", "15");
        Add("manifest_version_code", AuthenticationVersionCode);
        Add("resolution", AuthenticationResolution);
        Add("dpi", "280");
        Add("update_version_code", AuthenticationVersionCode);
        Add("_rticket", ticket.ToString(CultureInfo.InvariantCulture));
        Add("normal_session_cnt_in_day", "1");
        Add("gender", "0");
        Add("cold_start_session_cnt_in_day", "1");
        Add("host_abi", "arm64-v8a");
        Add("dragon_device_type", "pad");
        Add("sys_mini_window", "0");
        Add("pv_player", AuthenticationVersionCode);
        Add("app_mini_window", "0");
        Add("normal_session_id", _cdid + "#1");
        Add("compliance_status", "0");
        Add("har_status", "3");
        Add("cold_start_session_id", _cdid);
        Add("cold_start_session_cnt_in_life", "1");
        Add("charging", "0");
        Add("normal_session_cnt_in_life", "1");
        Add("is_power_save_mode", "0");
        Add("app_dark_mode", "0");
        Add("screen_brightness", "50");
        Add("battery_pct", "100");
        Add("down_speed", "60000");
        Add("sys_dark_mode", "0");
        Add("need_personal_recommend", "1");
        Add("player_so_load", "1");
        Add("font_scale", "100");
        Add("is_android_pad_screen", "1");
        Add("network_type", "4");
        Add("rom_version", AuthenticationRomVersion);
        Add("current_volume", "100");
        Add("cdid", _cdid);
        return [.. result];

        void Add(string key, string value)
        {
            if (!result.Any(pair => string.Equals(pair.Key, key, StringComparison.Ordinal)))
            {
                result.Add(new(key, value));
            }
        }
    }

    private void EnsureAuthenticationSession()
    {
        if (string.IsNullOrWhiteSpace(_cdid))
        {
            _cdid = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(_passportCsrfToken))
        {
            Span<byte> token = stackalloc byte[16];
            RandomNumberGenerator.Fill(token);
            _passportCsrfToken = Convert.ToHexString(token).ToLowerInvariant();
        }

        _cookies.TryAdd("install_id", _installId);
        _cookies.TryAdd("store-region", "cn-ah");
        _cookies.TryAdd("store-region-src", "did");
        _cookies.TryAdd("ttreq", _ttReq);
        _cookies.TryAdd("passport_csrf_token", _passportCsrfToken);
        _cookies.TryAdd("passport_csrf_token_default", _passportCsrfToken);
        _authHeaders["x-tt-passport-csrf-token"] = _passportCsrfToken;
        SaveSessionSnapshot();
    }

    private void LoadSessionSnapshot()
    {
        if (_sessionStateFilePath is null || !File.Exists(_sessionStateFilePath))
        {
            return;
        }

        try
        {
            AppSessionSnapshot? snapshot = JsonAotSerializer.Deserialize<AppSessionSnapshot>(
                File.ReadAllText(_sessionStateFilePath)
            );
            if (snapshot is null)
            {
                return;
            }
            foreach ((string key, string value) in snapshot.Cookies)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    _cookies[key] = value;
                }
            }
            foreach ((string key, string value) in snapshot.Headers)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    _authHeaders[key] = value;
                }
            }
            _cdid = snapshot.Cdid;
            if (DeviceIdGenerator.IsValid(snapshot.DeviceId))
            {
                _deviceId = snapshot.DeviceId;
            }
            if (DeviceIdGenerator.IsValid(snapshot.InstallId))
            {
                _installId = snapshot.InstallId;
            }
            _klinkEgdi = string.IsNullOrWhiteSpace(snapshot.KlinkEgdi)
                ? _klinkEgdi
                : snapshot.KlinkEgdi;
            _passportCsrfToken = snapshot.PassportCsrfToken;
            _ttReq = snapshot.TtReq;
            _userId = snapshot.UserId;
            _userName = snapshot.UserName;
            _userAvatarUrl = snapshot.UserAvatarUrl;
            _userDescription = snapshot.UserDescription;
            _userMaskedPhone = snapshot.UserMaskedPhone;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A stale or inaccessible optional snapshot must not prevent anonymous use.
        }
    }

    private void SaveSessionSnapshot(
        string? verificationTicket = null,
        string? maskedPhone = null,
        int? retryAfterSeconds = null
    )
    {
        if (_sessionStateFilePath is null)
        {
            return;
        }

        lock (_sessionFileLock)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_sessionStateFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AppSessionSnapshot old = File.Exists(_sessionStateFilePath)
                    ? JsonAotSerializer.Deserialize<AppSessionSnapshot>(
                        File.ReadAllText(_sessionStateFilePath)
                    ) ?? new()
                    : new();
                AppSessionSnapshot snapshot = new()
                {
                    Cookies = new Dictionary<string, string>(
                        _cookies,
                        StringComparer.OrdinalIgnoreCase
                    ),
                    Headers = new Dictionary<string, string>(
                        _authHeaders,
                        StringComparer.OrdinalIgnoreCase
                    ),
                    VerificationTicket = verificationTicket ?? old.VerificationTicket,
                    MaskedPhone = maskedPhone ?? old.MaskedPhone,
                    RetryAfterSeconds = retryAfterSeconds ?? old.RetryAfterSeconds,
                    Cdid = _cdid,
                    KlinkEgdi = _klinkEgdi,
                    PassportCsrfToken = _passportCsrfToken,
                    TtReq = _ttReq,
                    DeviceId = _deviceId,
                    InstallId = _installId,
                    UserId = _userId,
                    UserName = _userName,
                    UserAvatarUrl = _userAvatarUrl,
                    UserDescription = _userDescription,
                    UserMaskedPhone = _userMaskedPhone,
                    UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
                string temporary = _sessionStateFilePath + ".tmp";
                File.WriteAllText(temporary, JsonAotSerializer.Serialize(snapshot));
                File.Move(temporary, _sessionStateFilePath, overwrite: true);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // Session persistence is best effort; network calls remain usable when the path is unavailable.
            }
        }
    }

    internal void DeleteSessionSnapshot()
    {
        if (_sessionStateFilePath is null)
        {
            return;
        }

        lock (_sessionFileLock)
        {
            foreach (
                string path in new[]
                {
                    _sessionStateFilePath,
                    _sessionStateFilePath + ".response.json",
                    _sessionStateFilePath + ".tmp",
                }
            )
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception exception)
                    when (exception is IOException or UnauthorizedAccessException)
                {
                    // Best effort cleanup; the in-memory session is already cleared.
                }
            }
        }
    }

    private void SaveAuthenticationResponseDiagnostic(
        string path,
        HttpResponseMessage response,
        byte[] body
    )
    {
        if (_sessionStateFilePath is null || !path.Contains("/passport/", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            string diagnosticPath = _sessionStateFilePath + ".response.json";
            string headers = string.Join(
                "\n",
                response.Headers.SelectMany(static pair =>
                    pair.Value.Select(value => $"{pair.Key}: {value}")
                )
            );
            File.WriteAllText(
                diagnosticPath,
                $"Path: {path}\nStatus: {(int)response.StatusCode}\nHeaders:\n{headers}\nBody:\n{System.Text.Encoding.UTF8.GetString(body)}\n"
            );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { }
    }
}
