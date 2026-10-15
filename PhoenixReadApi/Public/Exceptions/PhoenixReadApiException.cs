using System.Net;

namespace PhoenixReadApi;

/// <summary>Base exception for a rejected, unavailable or malformed Phoenix response.</summary>
/// <remarks>
/// The original endpoint and integer status-code properties are kept for source compatibility.
/// New code can use <see cref="Operation"/>, <see cref="RequestUri"/>,
/// <see cref="ProviderCode"/> and <see cref="IsTransient"/> to decide how to handle a failure.
/// </remarks>
public class PhoenixReadApiException(
    string message,
    string operation,
    string? endpoint,
    Uri? requestUri,
    int? statusCode,
    string? providerCode,
    bool isTransient,
    Exception? innerException = null
) : Exception(message, innerException)
{
    /// <summary>Creates an API exception using the legacy endpoint/status-code shape.</summary>
    public PhoenixReadApiException(
        string message,
        string? endpoint = null,
        int? statusCode = null,
        Exception? innerException = null
    )
        : this(
            message,
            operation: endpoint ?? "unknown",
            endpoint,
            requestUri: null,
            statusCode,
            providerCode: null,
            isTransient: false,
            innerException
        ) { }

    /// <summary>The logical operation that failed.</summary>
    public string Operation { get; } = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation;

    /// <summary>The logical endpoint that failed.</summary>
    public string? Endpoint { get; } = endpoint;

    /// <summary>The request URI, when a request was created.</summary>
    public Uri? RequestUri { get; } = requestUri;

    /// <summary>The HTTP status code, when a response was received.</summary>
    public int? StatusCode { get; } = statusCode;

    /// <summary>The provider-specific error code, when one was returned.</summary>
    public string? ProviderCode { get; } = providerCode;

    /// <summary>Gets whether retrying the operation is normally safe.</summary>
    public bool IsTransient { get; } = isTransient;

    /// <summary>Gets the status code as a framework enum when available.</summary>
    public HttpStatusCode? HttpStatusCode => StatusCode is { } value ? (HttpStatusCode)value : null;

    /// <summary>Alias for <see cref="HttpStatusCode"/> for callers preferring a short name.</summary>
    public HttpStatusCode? Status => HttpStatusCode;
}
