namespace PhoenixReadApi;

/// <summary>当可恢复操作配置的所有数据源都失败时抛出此异常。</summary>
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
    ) { }
