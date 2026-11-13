namespace PhoenixReadApi;

/// <summary>关注追更短剧的单条状态与更新进度信息。</summary>
public sealed class FollowUpdateItem
{
    /// <summary>剧集 ID。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>当前已更新总集数。</summary>
    public int TotalEpisodeCount { get; init; }

    /// <summary>最近观看过的分集序号（从 1 开始，0 表示未观看）。</summary>
    public int LatestWatchedEpisodeIndex { get; init; }

    /// <summary>是否已观看到最新更新的分集。</summary>
    public bool HasWatchedLatest { get; init; }

    /// <summary>剧集是否已完结。</summary>
    public bool IsFinished { get; init; }

    /// <summary>更新频率文案（如“每周五更新”）。</summary>
    public string UpdateFrequency { get; init; } = string.Empty;

    /// <summary>关注追更的时间戳（Unix 毫秒）。</summary>
    public long FollowedAtUnixMilliseconds { get; init; }

    /// <summary>最新一集审核通过发布的时间戳（Unix 毫秒）。</summary>
    public long LastReleasedAtUnixMilliseconds { get; init; }
}
