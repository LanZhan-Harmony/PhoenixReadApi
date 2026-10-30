namespace PhoenixReadApi;

/// <summary>当找不到可用的媒体版本或内容密钥时抛出此异常。</summary>
public class PhoenixReadApiMediaException(
    string message,
    string? endpoint = "media",
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "media",
        endpoint,
        null,
        null,
        null,
        false,
        innerException
    );
