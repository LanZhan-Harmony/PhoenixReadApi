namespace PhoenixReadApi;

/// <summary>一部剧集中的一集。</summary>
public sealed class Episode
{
    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>从 1 开始的集数。</summary>
    public int Number { get; init; }

    /// <summary>分集标题。</summary>
    public string Title { get; init; } = string.Empty;
}
