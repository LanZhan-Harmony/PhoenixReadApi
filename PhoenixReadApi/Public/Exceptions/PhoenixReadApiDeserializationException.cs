namespace PhoenixReadApi;

/// <summary>当响应无法按照约定类型完成反序列化时抛出此异常。</summary>
public class PhoenixReadApiDeserializationException(
    string message,
    string? endpoint = null,
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "serialization",
        endpoint,
        null,
        null,
        null,
        false,
        innerException
    );
