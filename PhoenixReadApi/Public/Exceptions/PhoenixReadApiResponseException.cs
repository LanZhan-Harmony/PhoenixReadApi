namespace PhoenixReadApi;

/// <summary>当响应的 HTTP 状态码有效但内容结构不符合预期时抛出此异常。</summary>
public class PhoenixReadApiResponseException(
    string message,
    string? endpoint = null,
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "response",
        endpoint,
        null,
        null,
        null,
        false,
        innerException
    );
