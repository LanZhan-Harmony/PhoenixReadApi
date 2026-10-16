namespace PhoenixReadApi;

/// <summary>按清晰度排序的媒体版本。</summary>
public sealed class MediaVariant
{
    /// <summary>可播放媒体 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>从媒体标签解析出的清晰度分数。</summary>
    public int Quality { get; init; }

    /// <summary>可选的 CENC 解密密钥。</summary>
    public string DecryptionKey { get; init; } = string.Empty;
}

