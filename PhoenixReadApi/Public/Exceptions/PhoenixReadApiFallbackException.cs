namespace PhoenixReadApi;

/// <summary>当请求在可恢复失败后仍无法完成时抛出此异常。</summary>
public class PhoenixReadApiFallbackException(
    string message,
    string? endpoint = "fallback",
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "fallback",
        endpoint,
        null,
        null,
        null,
        false,
        innerException
    );
