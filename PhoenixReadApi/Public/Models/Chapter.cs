namespace PhoenixReadApi;

/// <summary>一部剧集中的分集信息。</summary>
public sealed class Chapter
{
    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>分集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>从 1 开始的分集编号。</summary>
    public int Episode { get; init; }
}
