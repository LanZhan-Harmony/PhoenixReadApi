namespace PhoenixReadApi;

/// <summary>Thrown when Phoenix returns a provider-level error code.</summary>
public class PhoenixReadApiProviderException(
    string message,
    string? endpoint = null,
    string? providerCode = null,
    bool isTransient = true,
    Exception? innerException = null
)
    : PhoenixReadApiException(
        message,
        endpoint ?? "provider",
        endpoint,
        null,
        null,
        providerCode,
        isTransient,
        innerException
    ) { }

/// <summary>Thrown when the provider rejects the current authentication/device.</summary>
/// <remarks>Creates an authentication or device rejection exception.</remarks>
public sealed class PhoenixReadApiAuthenticationException(
    string message,
    string? endpoint = null,
    string? providerCode = null,
    Exception? innerException = null
)
    : PhoenixReadApiProviderException(
        message,
        endpoint,
        providerCode,
        isTransient: false,
        innerException
    ) { }
