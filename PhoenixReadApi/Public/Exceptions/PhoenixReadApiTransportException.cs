namespace PhoenixReadApi;

/// <summary>Thrown when the request could not be sent or timed out.</summary>
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
