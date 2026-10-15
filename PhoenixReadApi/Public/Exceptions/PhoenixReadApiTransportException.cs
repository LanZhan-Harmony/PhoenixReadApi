namespace PhoenixReadApi;

/// <summary>当请求无法发送或发生超时时抛出此异常。</summary>
public class PhoenixReadApiTransportException(
    string message,
    string? endpoint = null,
    Uri? requestUri = null,
    bool isTransient = true,
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "transport",
        endpoint,
        requestUri,
        null,
        null,
        isTransient,
        innerException
    ) { }
