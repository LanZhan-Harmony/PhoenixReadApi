using System.Net.Http.Headers;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>获取大小受限的红果 HTML 页面，并应用共享重试策略。</summary>
internal sealed class PhoenixReadWebTransport
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private static readonly MediaTypeWithQualityHeaderValue HtmlAcceptHeader = new("text/html");

    internal PhoenixReadWebTransport(PhoenixReadHttpTransport http, PhoenixReadApiOptions options)
    {
        _http = http;
        _options = options;
    }

    internal Task<string> GetStringAsync(
        Uri endpoint,
        Uri referer,
        string operation,
        CancellationToken cancellationToken,
        int? maxBytes = null
    ) =>
        RetryPolicy.ExecuteAsync(
            (_, token) => GetOnceAsync(endpoint, referer, operation, maxBytes, token),
            Math.Min(_options.RetryCount, 2),
            RetryPolicy.IsTransient,
            cancellationToken
        );

    private async Task<string> GetOnceAsync(
        Uri endpoint,
        Uri referer,
        string operation,
        int? maxBytes,
        CancellationToken cancellationToken
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.Accept.Add(HtmlAcceptHeader);
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9");
        request.Headers.Referrer = referer;
        (HttpResponseMessage response, byte[] body) = await _http
            .SendAndReadAsync(request, cancellationToken, null, maxBytes)
            .ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new PhoenixReadApiHttpException(
                    $"{operation} 接口 HTTP {(int)response.StatusCode}。",
                    operation,
                    (int)response.StatusCode,
                    endpoint,
                    isTransient: (int)response.StatusCode >= 500
                );
            }

            return Encoding.UTF8.GetString(body);
        }
    }
}
