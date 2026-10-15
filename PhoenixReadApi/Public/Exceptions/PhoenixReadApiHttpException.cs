namespace PhoenixReadApi;

/// <summary>当 HTTP 响应状态码表示失败时抛出此异常。</summary>
public class PhoenixReadApiHttpException(
    string message,
    string? endpoint = null,
    int? statusCode = null,
    Uri? requestUri = null,
    bool isTransient = false,
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "http",
        endpoint,
        requestUri,
        statusCode,
        null,
        isTransient,
        innerException
    ) { }
