using System.Net;

namespace PhoenixReadApi;

/// <summary>红果响应被拒绝、不可用或格式错误时使用的基础异常类型。</summary>
/// <remarks>
/// 为保持源代码兼容性，继续保留原有的接口和整数状态码属性。
/// 新代码可以使用 <see cref="Operation"/>、<see cref="RequestUri"/>、
/// <see cref="ProviderCode"/> 和 <see cref="IsTransient"/> 判断如何处理失败。
/// </remarks>
public class PhoenixReadApiException(
    string message,
    string operation,
    string? endpoint,
    Uri? requestUri,
    int? statusCode,
    string? providerCode,
    bool isTransient,
    Exception? innerException = null
) : Exception(message, innerException)
{
    /// <summary>使用旧版接口和状态码参数创建 API 异常。</summary>
    public PhoenixReadApiException(
        string message,
        string? endpoint = null,
        int? statusCode = null,
        Exception? innerException = null
    )
        : this(
            message,
            operation: endpoint ?? "unknown",
            endpoint,
            requestUri: null,
            statusCode,
            providerCode: null,
            isTransient: false,
            innerException
        ) { }

    /// <summary>失败的逻辑操作名称。</summary>
    public string Operation { get; } = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation;

    /// <summary>发生失败的逻辑接口。</summary>
    public string? Endpoint { get; } = endpoint;

    /// <summary>发起请求时使用的 URI。</summary>
    public Uri? RequestUri { get; } = requestUri;

    /// <summary>收到响应时的 HTTP 状态码。</summary>
    public int? StatusCode { get; } = statusCode;

    /// <summary>红果接口返回的错误码。</summary>
    public string? ProviderCode { get; } = providerCode;

    /// <summary>获取该操作是否通常可以安全重试。</summary>
    public bool IsTransient { get; } = isTransient;

    /// <summary>状态码可用时，以框架枚举返回该状态码。</summary>
    public HttpStatusCode? HttpStatusCode => StatusCode is { } value ? (HttpStatusCode)value : null;

    /// <summary>为需要简短名称的调用方提供的 <see cref="HttpStatusCode"/> 别名。</summary>
    public HttpStatusCode? Status => HttpStatusCode;
}
