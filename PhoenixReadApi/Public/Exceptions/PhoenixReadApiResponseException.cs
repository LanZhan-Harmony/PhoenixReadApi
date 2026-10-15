namespace PhoenixReadApi;

/// <summary>Thrown when a response has a valid HTTP status but an unexpected shape.</summary>
public class PhoenixReadApiResponseException(
    string message,
    string? endpoint = null,
    Exception? innerException = null
) : PhoenixReadApiDeserializationException(message, endpoint, innerException) { }
