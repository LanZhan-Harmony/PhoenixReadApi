namespace PhoenixReadApi;

/// <summary>
/// PhoenixReadApi 的主入口类，提供完整的异步客户端功能。
/// </summary>
public sealed class PhoenixReadApi : PhoenixReadApiClient
{
    /// <summary>使用默认接口地址创建客户端。</summary>
    public PhoenixReadApi(PhoenixReadApiOptions? options = null)
        : base(options) { }

    /// <summary>使用由应用管理的 HTTP 客户端创建客户端。</summary>
    public PhoenixReadApi(HttpClient httpClient, PhoenixReadApiOptions? options = null)
        : base(httpClient, options) { }
}
