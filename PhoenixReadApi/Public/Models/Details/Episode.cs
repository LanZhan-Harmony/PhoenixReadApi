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

    /// <summary>分集时长。</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;

    /// <summary>分集剧照或封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>是否可播放（当服务端标记限制或下架时为 false）。</summary>
    public bool Playable { get; init; } = true;

    /// <summary>是否为竖屏画幅。</summary>
    public bool IsVertical { get; init; }

    /// <summary>是否为本次更新中发布的新分集。</summary>
    public bool IsNewlyUpdated { get; init; }

    /// <summary>分集评论数。</summary>
    public long CommentCount { get; init; }

    /// <summary>分集点赞数。</summary>
    public long DiggCount { get; init; }
}
