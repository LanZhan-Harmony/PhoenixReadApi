using PhoenixReadApi.Internal.Infrastructure.Http;

namespace PhoenixReadApi.Internal.Infrastructure.Runtime;

/// <summary>
/// 单个客户端实例的共享运行时状态。各功能服务都依赖此运行时
/// 而不直接依赖公共客户端入口，以便集中管理传输层。
/// </summary>
internal sealed class PhoenixReadClientRuntime : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    internal PhoenixReadClientRuntime(
        HttpClient httpClient,
        PhoenixReadApiOptions? options,
        bool ownsHttpClient
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        Options = (options ?? new PhoenixReadApiOptions()).Normalize();
        PhoenixReadHttpTransport httpTransport = new(_httpClient, Options);
        AppTransport = new PhoenixReadAppTransport(httpTransport, Options);
        WebTransport = new PhoenixReadWebTransport(httpTransport, Options);
    }

    internal PhoenixReadApiOptions Options { get; }

    internal PhoenixReadAppTransport AppTransport { get; }

    internal PhoenixReadWebTransport WebTransport { get; }

    internal Task<string> GetTextAsync(
        Uri endpoint,
        Uri refererBase,
        string operation,
        CancellationToken cancellationToken,
        int? maxBytes = null
    ) => WebTransport.GetStringAsync(endpoint, refererBase, operation, cancellationToken, maxBytes);

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
