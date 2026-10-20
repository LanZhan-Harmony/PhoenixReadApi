using PhoenixReadApi.Internal.Infrastructure.Crypto;

namespace PhoenixReadApi;

/// <summary>PhoenixReadApiClient 的配置选项。</summary>
public sealed class PhoenixReadApiOptions
{
    /// <summary>内容服务的基础地址。</summary>
    public Uri WebBaseUri { get; init; } = new("https://hongguoduanju.com/", UriKind.Absolute);

    /// <summary>数据服务的基础地址。</summary>
    public Uri AppBaseUri { get; init; } =
        new("https://api5-normal-sinfonlinea.fqnovel.com/", UriKind.Absolute);

    /// <summary>Passport 登录接口的基础地址。</summary>
    /// <remarks>普通内容请求仍使用 <see cref="AppBaseUri"/>；该地址只用于短信登录链路。</remarks>
    public Uri AuthenticationBaseUri { get; init; } =
        new("https://api5-normal-hl.fqnovel.com/", UriKind.Absolute);

    /// <summary>可选的登录会话备份文件路径。</summary>
    /// <remarks>
    /// 文件包含 Cookie、Passport Token 和短信票据等敏感信息，应用应限制文件访问权限。
    /// 不能与 <see cref="AnonymousStateFilePath"/> 同时设置。
    /// </remarks>
    public string? AuthenticationStateFilePath { get; init; }

    /// <summary>可选的匿名设备会话备份文件路径。</summary>
    /// <remarks>
    /// 用于在不登录的情况下持续使用同一个设备会话和匿名用户 ID；不能与
    /// <see cref="AuthenticationStateFilePath"/> 同时设置。
    /// </remarks>
    public string? AnonymousStateFilePath { get; init; }

    /// <summary>每个分类最多请求的剧库页数。</summary>
    public int MaxPagesPerCategory { get; init; } = 50;

    /// <summary>读取请求遇到临时性错误时的最大尝试次数，包含首次请求；1 表示不重试。</summary>
    public int RetryCount { get; init; } = 2;

    /// <summary>单次请求的超时时间；如果传入取消令牌，则以取消令牌为准。</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>常规接口允许接收的最大响应体大小。</summary>
    public int MaxResponseBytes { get; init; } = 8 * 1024 * 1024;

    /// <summary>主要请求失败时是否继续尝试获取内容。</summary>
    public bool EnableWebFallback { get; init; } = true;

    /// <summary>搜索时是否允许在限定请求预算内补齐缺失的季数。</summary>
    public bool CompleteSearchSeasons { get; init; } = true;

    /// <summary>用于请求的可选持久化设备 ID（数字字符串）。</summary>
    public string? DeviceId { get; init; }

    /// <summary>用于请求的可选持久化安装 ID（数字字符串）。</summary>
    public string? InstallId { get; init; }

    /// <summary>发送请求时使用的应用 User-Agent。</summary>
    public string UserAgent { get; init; } =
        "com.phoenix.read/73932 (Linux; U; Android 15; zh_CN_#Hans; AOSP on Intel Platform; Build/SQ1A.220205.002;tt-ok/3.12.13.20)";

    internal PhoenixReadApiOptions Normalize()
    {
        string? authenticationPath = NormalizeStatePath(AuthenticationStateFilePath);
        string? anonymousPath = NormalizeStatePath(AnonymousStateFilePath);
        if (authenticationPath is not null && anonymousPath is not null)
        {
            throw new ArgumentException(
                "AuthenticationStateFilePath 和 AnonymousStateFilePath 不能同时设置。"
            );
        }
        return new PhoenixReadApiOptions
        {
            WebBaseUri = NormalizeBase(WebBaseUri, "https://hongguoduanju.com/"),
            AppBaseUri = NormalizeBase(AppBaseUri, "https://api5-normal-sinfonlinea.fqnovel.com/"),
            AuthenticationBaseUri = NormalizeBase(AuthenticationBaseUri, "https://api5-normal-hl.fqnovel.com/"),
            AuthenticationStateFilePath = authenticationPath,
            AnonymousStateFilePath = anonymousPath,
            MaxPagesPerCategory = Math.Clamp(MaxPagesPerCategory, 1, 500),
            RetryCount = Math.Clamp(RetryCount, 1, 3),
            RequestTimeout =
                RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(5)
                    ? TimeSpan.FromSeconds(20)
                    : RequestTimeout,
            MaxResponseBytes = Math.Clamp(MaxResponseBytes, 16 * 1024, 64 * 1024 * 1024),
            EnableWebFallback = EnableWebFallback,
            CompleteSearchSeasons = CompleteSearchSeasons,
            // Every client instance receives its own device identity. It is persisted
            // in the optional session snapshot when a state path is configured.
            DeviceId = NormalizeNumericId(DeviceId) ?? DeviceIdGenerator.Create(),
            InstallId = NormalizeNumericId(InstallId) ?? DeviceIdGenerator.Create(),
            UserAgent = string.IsNullOrWhiteSpace(UserAgent)
                ? "com.phoenix.read/73932 (Linux; U; Android 15; zh_CN_#Hans; AOSP on Intel Platform; Build/SQ1A.220205.002;tt-ok/3.12.13.20)"
                : UserAgent.Trim(),
        };
    }

    private static Uri NormalizeBase(Uri? value, string fallback)
    {
        Uri uri = ValidateAbsolute(value ?? new Uri(fallback, UriKind.Absolute), nameof(value));
        return new Uri(
            uri.AbsoluteUri.EndsWith('/') ? uri.AbsoluteUri : uri.AbsoluteUri + "/",
            UriKind.Absolute
        );
    }

    private static Uri ValidateAbsolute(Uri? value, string name)
    {
        if (
            value is null
            || !value.IsAbsoluteUri
            || (value.Scheme != Uri.UriSchemeHttp && value.Scheme != Uri.UriSchemeHttps)
        )
        {
            throw new ArgumentException($"{name} must be an absolute HTTP(S) URI.", name);
        }

        return value;
    }

    private static string? NormalizeNumericId(string? value)
    {
        return value is not null && DeviceIdGenerator.IsValid(value) ? value : null;
    }

    private static string? NormalizeStatePath(string? value)
    {
        value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return value is null || value.IndexOfAny(['\0', '\r', '\n']) >= 0 ? null : value;
    }
}
