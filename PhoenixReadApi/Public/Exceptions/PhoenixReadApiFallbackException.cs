namespace PhoenixReadApi;

/// <summary>Thrown when all configured providers fail for a recoverable operation.</summary>
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
