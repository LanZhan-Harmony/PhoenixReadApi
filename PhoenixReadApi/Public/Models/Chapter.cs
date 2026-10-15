namespace PhoenixReadApi;

/// <summary>一部剧集中的分集信息。</summary>
public class Chapter
{
    /// <summary>跨数据源通用的分集 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>固定的数据源名称。</summary>
    public string Source { get; init; } = "hongguo";

    /// <summary>分集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>红果接口返回的视频 ID 或 URL。</summary>
    public string VideoUrl { get; init; } = string.Empty;

    /// <summary>从 1 开始的分集编号。</summary>
    public int Episode { get; init; }

    /// <summary>网页 URL。</summary>
    public string PageUrl { get; init; } = string.Empty;

    /// <summary>红果接口要求的 Referer。</summary>
    public string Referer { get; init; } = string.Empty;
}

