namespace PhoenixReadApi;

/// <summary>可播放的媒体地址，以及可选的 CENC 密钥。</summary>
public sealed class PlaybackMedia
{
    /// <summary>可播放媒体 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>播放器要求的 Referer。</summary>
    public string Referer { get; init; } = string.Empty;

    /// <summary>媒体时长。</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>从媒体标签解析出的清晰度分数。</summary>
    public int Quality { get; init; }

    /// <summary>可选的 CENC 解密密钥。</summary>
    public string DecryptionKey { get; init; } = string.Empty;

    /// <summary>可用的媒体版本。</summary>
    public IReadOnlyList<MediaVariant> Variants { get; init; } = [];
}

