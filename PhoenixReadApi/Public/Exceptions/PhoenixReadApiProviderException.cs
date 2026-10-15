namespace PhoenixReadApi;

/// <summary>当红果接口返回业务错误码时抛出此异常。</summary>
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

/// <summary>当红果接口拒绝当前设备或身份验证信息时抛出此异常。</summary>
/// <remarks>身份验证信息或设备被拒绝时使用的异常类型。</remarks>
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
