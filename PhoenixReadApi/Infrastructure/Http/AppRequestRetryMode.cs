namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>App 请求的重试语义。</summary>
internal enum AppRequestRetryMode
{
    /// <summary>读取接口可以按配置重试。</summary>
    ReadOnly,

    /// <summary>服务端定义为幂等的写入接口可以按配置重试。</summary>
    IdempotentWrite,

    /// <summary>可能产生副作用的接口只发送一次。</summary>
    NoRetry,
}
