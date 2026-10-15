namespace PhoenixReadApi;

/// <summary>Thrown when a response cannot be deserialized into its contract.</summary>
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
    ) { }
