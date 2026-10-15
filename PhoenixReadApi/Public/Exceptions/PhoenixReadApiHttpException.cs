namespace PhoenixReadApi;

/// <summary>Thrown when an HTTP response has an unsuccessful status code.</summary>
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
