using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.Web.Auth;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>负责处理红果 Passport 认证和表单接口传输。</summary>
internal sealed class PhoenixReadPassportTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private readonly PhoenixReadDeviceManager _deviceManager;
    private readonly PhoenixReadSessionStore _sessionStore;

    internal PhoenixReadPassportTransport(
        PhoenixReadHttpTransport http,
        PhoenixReadApiOptions options,
        PhoenixReadDeviceManager deviceManager,
        PhoenixReadSessionStore sessionStore
    )
    {
        _http = http;
        _options = options;
        _deviceManager = deviceManager;
        _sessionStore = sessionStore;
    }

    /// <summary>发送登录等接口使用的 application/x-www-form-urlencoded 请求。</summary>
    internal Task<AppResponse<TResponse>> PostFormAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> values,
        CancellationToken cancellationToken
    )
    {
        _sessionStore.EnsureAuthenticationSession();
        KeyValuePair<string, string>[] items = BuildAuthenticationFormValues(path, values);
        string encoded = string.Join(
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

    private async Task<AppResponse<TResponse>> SendFormOnceAsync<TResponse>(
        string path,
        byte[] body,
        CancellationToken cancellationToken
    )
    {
        Uri endpoint = BuildAuthenticationUri(path);
        Uri requestUri = HttpUriHelper.AddQuery(
            endpoint,
            "_rticket",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "com.phoenix.read/69132 (Linux; U; Android 17; zh_CN_#Hans; WKL-AN20; Build/CP2A.260605.012;tt-ok/3.12.13.20)"
        );
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("Sdk-Version", "2");
        request.Headers.TryAddWithoutValidation("Passport-Sdk-Version", "5051452");
        request.Headers.TryAddWithoutValidation("X-VC-Bdturing-Sdk-Version", "4.0.3.cn");
        request.Headers.TryAddWithoutValidation("lc", "101");
        _sessionStore.ApplySession(request);
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
        _sessionStore.CaptureSession(response);
        byte[] bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _sessionStore.SaveAuthenticationResponseDiagnostic(path, response, bytes);
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

    private Uri BuildAuthenticationUri(string path)
    {
        long ticket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        KeyValuePair<string, string>[] authenticationQuery =
        [
            new("passport-sdk-version", "5051452"),
            new("use_new_token_expire_rule", "true"),
            new("klink_egdi", _deviceManager.KlinkEgdi),
            new("iid", _deviceManager.InstallId),
            new("device_id", _deviceManager.DeviceId),
            new("ac", "wifi"),
            new("channel", "69310258"),
            new("aid", "8662"),
            new("app_name", "novelread"),
            new("version_code", "69132"),
            new("version_name", "6.9.1.32"),
            new("device_platform", "android"),
            new("os", "android"),
            new("ssmix", "a"),
            new("device_type", "WKL-AN20"),
            new("device_brand", "HONOR"),
            new("language", "zh"),
            new("os_api", "37"),
            new("os_version", "17"),
            new("manifest_version_code", "69132"),
            new("resolution", "1320*2868"),
            new("dpi", "360"),
            new("update_version_code", "69132"),
            new("_rticket", ticket.ToString(CultureInfo.InvariantCulture)),
            new("host_abi", "arm64-v8a"),
            new("dragon_device_type", "pad"),
            new("pv_player", "69132"),
            new("compliance_status", "0"),
            new("need_personal_recommend", "1"),
            new("player_so_load", "1"),
            new("rom_version", "CP2A.260605.012"),
            new("cdid", _deviceManager.Cdid),
            new("okhttp_version", "4.2.243.31-douyin"),
            new("use_store_region_cookie", "1"),
        ];
        return HttpUriHelper.CreateRawUri(
            _options.AuthenticationBaseUri,
            path,
            authenticationQuery
        );
    }

    private KeyValuePair<string, string>[] BuildAuthenticationFormValues(
        string path,
        IEnumerable<KeyValuePair<string, string>> values
    )
    {
        List<KeyValuePair<string, string>> result = [.. values];
        long ticket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Add("klink_egdi", _deviceManager.KlinkEgdi);
        Add("iid", _deviceManager.InstallId);
        Add("device_id", _deviceManager.DeviceId);
        Add("ac", "wifi");
        Add("channel", "69310258");
        Add("aid", "8662");
        Add("app_name", "novelread");
        Add("version_code", "69132");
        Add("version_name", "6.9.1.32");
        Add("device_platform", "android");
        Add("os", "android");
        Add("ssmix", "a");
        Add("device_type", "WKL-AN20");
        Add("device_brand", "HONOR");
        Add("language", "zh");
        Add("os_api", "37");
        Add("os_version", "17");
        Add("manifest_version_code", "69132");
        Add("resolution", "1320*2868");
        Add("dpi", "360");
        Add("update_version_code", "69132");
        Add("_rticket", ticket.ToString(CultureInfo.InvariantCulture));
        Add("normal_session_cnt_in_day", "1");
        Add("gender", "0");
        Add("cold_start_session_cnt_in_day", "1");
        Add("host_abi", "arm64-v8a");
        Add("dragon_device_type", "pad");
        Add("sys_mini_window", "0");
        Add("pv_player", "69132");
        Add("app_mini_window", "0");
        Add("normal_session_id", _deviceManager.Cdid + "#1");
        Add("compliance_status", "0");
        Add("har_status", "3");
        Add("cold_start_session_id", _deviceManager.Cdid);
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
        Add("rom_version", "CP2A.260605.012");
        Add("current_volume", "100");
        Add("cdid", _deviceManager.Cdid);
        return [.. result];

        void Add(string key, string value)
        {
            if (!result.Any(pair => string.Equals(pair.Key, key, StringComparison.Ordinal)))
            {
                result.Add(new(key, value));
            }
        }
    }

    internal async Task<WebPassportResponse<TResponse>> GetWebJsonAsync<TResponse>(
        string path,
        IEnumerable<KeyValuePair<string, string>> query,
        CancellationToken cancellationToken
    )
    {
        List<KeyValuePair<string, string>> fullQuery =
        [
            new("aid", "513755"),
            new("passport_jssdk_version", "3.4.10"),
            new("passport_jssdk_type", "normal"),
            new("account_sdk_source", "web"),
            new("is_from_ttaccountsdk", "1"),
            new("language", "zh"),
            .. query,
        ];
        Uri endpoint = HttpUriHelper.CreateRawUri(
            new Uri("https://hongguoduanju.com"),
            path,
            fullQuery
        );
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
        );
        request.Headers.TryAddWithoutValidation("Referer", "https://hongguoduanju.com/");
        request.Headers.TryAddWithoutValidation("Origin", "https://hongguoduanju.com");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _sessionStore.ApplySession(request);
        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        _sessionStore.CaptureSession(response);
        byte[] bytes = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new PhoenixReadApiHttpException(
                $"红果 Web 认证接口 HTTP {(int)response.StatusCode}。",
                path,
                (int)response.StatusCode,
                endpoint,
                isTransient: (int)response.StatusCode >= 500
            );
        }

        try
        {
            WebPassportResponse<TResponse> result =
                JsonAotSerializer.Deserialize<WebPassportResponse<TResponse>>(bytes)
                ?? throw new JsonException("The Web passport response was null.");
            return result;
        }
        catch (JsonException exception)
        {
            throw new PhoenixReadApiDeserializationException(
                "红果 Web 认证接口返回格式异常。",
                path,
                exception
            );
        }
    }

    internal async Task FollowRedirectAsync(string redirectUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return;
        }

        Uri currentUri = new(new Uri("https://hongguoduanju.com"), redirectUrl);
        for (int i = 0; i < 5; i++)
        {
            if (
                currentUri.Scheme != "https"
                || !currentUri.Host.EndsWith("hongguoduanju.com", StringComparison.OrdinalIgnoreCase)
            )
            {
                break;
            }

            using HttpRequestMessage request = new(HttpMethod.Get, currentUri);
            request.Headers.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
            );
            request.Headers.TryAddWithoutValidation("Referer", "https://hongguoduanju.com/");
            _sessionStore.ApplySession(request);
            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            _sessionStore.CaptureSession(response);

            if (
                (int)response.StatusCode is 301 or 302 or 303 or 307 or 308
                && response.Headers.Location is not null
            )
            {
                currentUri = new Uri(currentUri, response.Headers.Location);
                continue;
            }
            break;
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
