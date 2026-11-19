namespace PhoenixReadApi;

/// <summary>分享链接及其对应的剧集和分集。</summary>
public sealed class ShareLink
{
    /// <summary>完整分享 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>短链接。</summary>
    public string ShortUrl { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>分享标题。</summary>
    public string Title { get; init; } = string.Empty;
}
