namespace PhoenixReadApi;

/// <summary>Options used by PhoenixReadApiClient.</summary>
public class PhoenixReadApiOptions
{
    /// <summary>The Phoenix web site base address.</summary>
    public Uri WebBaseUri { get; init; } = new("https://hongguoduanju.com/", UriKind.Absolute);

    /// <summary>The Phoenix App API base address.</summary>
    public Uri AppBaseUri { get; init; } =
        new("https://api5-normal-sinfonlineb.fqnovel.com/", UriKind.Absolute);

    /// <summary>The optional backup playback endpoint.</summary>
    public Uri PlaybackApiUri { get; init; } =
        new("https://djapi.999888456.xyz/api/hongguo/play", UriKind.Absolute);

    /// <summary>Maximum number of catalog pages requested for a category.</summary>
    public int MaxPagesPerCategory { get; init; } = 50;

    /// <summary>Number of attempts for transient App requests.</summary>
    public int RetryCount { get; init; } = 2;

    /// <summary>Per-request timeout. Cancellation tokens remain authoritative.</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Maximum response size accepted by regular endpoints.</summary>
    public int MaxResponseBytes { get; init; } = 8 * 1024 * 1024;

    /// <summary>Whether catalog, detail and playback calls may use web fallbacks.</summary>
    public bool EnableWebFallback { get; init; } = true;

    /// <summary>Whether playback may use the public backup endpoint.</summary>
    public bool EnablePlaybackApiFallback { get; init; } = true;

    /// <summary>Whether search may spend a bounded budget filling missing season numbers.</summary>
    public bool CompleteSearchSeasons { get; init; } = true;

    /// <summary>Optional persisted numeric device identifier used for App requests.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Optional persisted numeric install identifier used for App requests.</summary>
    public string? InstallId { get; init; }

    /// <summary>Application user agent sent to Phoenix endpoints.</summary>
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
