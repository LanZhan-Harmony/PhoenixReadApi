namespace PhoenixReadApi;

/// <summary>
/// PhoenixReadApi 的主入口名称，继承完整的异步客户端能力。
/// </summary>
public sealed class PhoenixReadApi : PhoenixReadApiClient
{
    /// <summary>Creates a client with default endpoints.</summary>
    public PhoenixReadApi(PhoenixReadApiOptions? options = null)
        : base(options) { }

    /// <summary>Creates a client using an application-owned HTTP client.</summary>
    public PhoenixReadApi(HttpClient httpClient, PhoenixReadApiOptions? options = null)
        : base(httpClient, options) { }
}
