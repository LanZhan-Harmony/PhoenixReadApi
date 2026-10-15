namespace PhoenixReadApi;

/// <summary>PhoenixReadApiClient 的配置选项。</summary>
public class PhoenixReadApiOptions
{
    /// <summary>红果网站的基础地址。</summary>
    public Uri WebBaseUri { get; init; } = new("https://hongguoduanju.com/", UriKind.Absolute);

    /// <summary>红果 App API 的基础地址。</summary>
    public Uri AppBaseUri { get; init; } =
        new("https://api5-normal-sinfonlineb.fqnovel.com/", UriKind.Absolute);

    /// <summary>可选的备用播放接口。</summary>
    public Uri PlaybackApiUri { get; init; } =
        new("https://djapi.999888456.xyz/api/hongguo/play", UriKind.Absolute);

    /// <summary>每个分类最多请求的剧库页数。</summary>
    public int MaxPagesPerCategory { get; init; } = 50;

    /// <summary>App 请求遇到临时性错误时的最大重试次数。</summary>
    public int RetryCount { get; init; } = 2;

    /// <summary>单次请求的超时时间；如果传入取消令牌，则以取消令牌为准。</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>常规接口允许接收的最大响应体大小。</summary>
    public int MaxResponseBytes { get; init; } = 8 * 1024 * 1024;

    /// <summary>剧库、详情和播放请求是否允许使用网页接口兜底。</summary>
    public bool EnableWebFallback { get; init; } = true;

    /// <summary>播放是否允许使用公共备用接口兜底。</summary>
    public bool EnablePlaybackApiFallback { get; init; } = true;

    /// <summary>搜索时是否允许在限定请求预算内补齐缺失的季数。</summary>
    public bool CompleteSearchSeasons { get; init; } = true;

    /// <summary>用于 App 请求的可选持久化设备 ID（数字字符串）。</summary>
    public string? DeviceId { get; init; }

    /// <summary>用于 App 请求的可选持久化安装 ID（数字字符串）。</summary>
    public string? InstallId { get; init; }

    /// <summary>发送给红果接口的应用 User-Agent。</summary>
    public string UserAgent { get; init; } =
        "com.phoenix.read/73532 (Linux; U; Android 16; zh_CN; 25053RT47C; Build/BP2A.250605.031.A3; Cronet/TTNetVersion:04657795 2026-01-23 QuicVersion:c67e9834 2025-09-08)";

    internal PhoenixReadApiOptions Normalize()
    {
        return new PhoenixReadApiOptions
        {
            WebBaseUri = NormalizeBase(WebBaseUri, "https://hongguoduanju.com/"),
            AppBaseUri = NormalizeBase(AppBaseUri, "https://api5-normal-sinfonlineb.fqnovel.com/"),
            PlaybackApiUri = ValidateAbsolute(PlaybackApiUri, "PlaybackApiUri"),
            MaxPagesPerCategory = Math.Clamp(MaxPagesPerCategory, 1, 500),
            RetryCount = Math.Clamp(RetryCount, 1, 3),
            RequestTimeout =
                RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(5)
                    ? TimeSpan.FromSeconds(20)
                    : RequestTimeout,
            MaxResponseBytes = Math.Clamp(MaxResponseBytes, 16 * 1024, 64 * 1024 * 1024),
            EnableWebFallback = EnableWebFallback,
            EnablePlaybackApiFallback = EnablePlaybackApiFallback,
            CompleteSearchSeasons = CompleteSearchSeasons,
            DeviceId = NormalizeNumericId(DeviceId) ?? DeviceIdGenerator.Create(),
            InstallId = NormalizeNumericId(InstallId) ?? DeviceIdGenerator.Create(),
            UserAgent = string.IsNullOrWhiteSpace(UserAgent)
                ? "PhoenixReadApi/1.0"
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
}
