namespace PhoenixReadApi;

/// <summary>阅读或观看历史中的一条记录。</summary>
/// <typeparam name="T">目标内容实体类型（如 <see cref="Drama"/>、<see cref="DramaAlbum"/>、<see cref="ShortVideo"/> 或 <see cref="Post"/>）。</typeparam>
public sealed class HistoryItem<T>
{
    /// <summary>服务端记录 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>关联的内容实体。</summary>
    public required T Target { get; init; }

    /// <summary>最近观看的视频 ID（剧集分集或合集子视频）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>集数或子视频序号。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>最近播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>视频时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>最近阅读或观看时间（Unix 毫秒）。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }
}
