using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>构建并签名红果 App 请求，然后反序列化响应。</summary>
internal sealed class PhoenixReadAppTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;

    internal PhoenixReadAppTransport(PhoenixReadHttpTransport http, PhoenixReadApiOptions options)
    {
        _http = http;
        _options = options;
    }

    internal Task<AppResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        bool commentRequest,
        CancellationToken cancellationToken
    )
    {
        var body = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        return RetryPolicy.ExecuteAsync(
            (attempt, token) => SendOnceAsync<TResponse>(path, body, commentRequest, token),
            _options.RetryCount,
            RetryPolicy.IsTransient,
            cancellationToken
        );
    }

    private async Task<AppResponse<TResponse>> SendOnceAsync<TResponse>(
        string path,
        byte[] body,
        bool commentRequest,
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
        if (commentRequest)
        {
            request.Headers.TryAddWithoutValidation("Comment-Source", "601");
            request.Headers.TryAddWithoutValidation("Server-Channel", "1000");
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
        else
        {
            PhoenixReadRequestSigner.Sign(request, body, now);
        }

        using HttpResponseMessage response = await _http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
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

    private Uri BuildAppUri(string path)
    {
        KeyValuePair<string, string>[] query =
        [
            new KeyValuePair<string, string>("aid", "8662"),
            new("app_name", "novelread"),
            new("version_code", "73532"),
            new("version_name", "7.3.5.32"),
            new("manifest_version_code", "73532"),
            new("update_version_code", "73532"),
            new("channel", "update_64"),
            new("device_platform", "android"),
            new("os", "android"),
            new("ssmix", "a"),
            new("device_type", "25053RT47C"),
            new("device_brand", "Redmi"),
            new("language", "zh"),
            new("os_api", "36"),
            new("os_version", "16"),
            new("resolution", "1280*2772"),
            new("dpi", "520"),
            new("ac", "wifi"),
            new("device_id", _options.DeviceId!),
            new("iid", _options.InstallId!),
        ];
        Uri baseUri = new(_options.AppBaseUri, path.TrimStart('/'));
        var queryText = string.Join(
            '&',
            query.Select(static pair =>
                Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)
            )
        );
        return new UriBuilder(baseUri) { Query = queryText }.Uri;
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
}
