namespace PhoenixReadApi;

/// <summary>Thrown when no usable media variant or content key is available.</summary>
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
    ) { }
