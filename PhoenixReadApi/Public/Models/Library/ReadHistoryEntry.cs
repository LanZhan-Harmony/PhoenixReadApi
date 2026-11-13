namespace PhoenixReadApi;

/// <summary>阅读历史中的一条短剧记录。</summary>
public sealed class ReadHistoryEntry
{
    /// <summary>服务端记录 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>集数。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>最近播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>视频时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>最近阅读时间（Unix 毫秒）。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }
}
