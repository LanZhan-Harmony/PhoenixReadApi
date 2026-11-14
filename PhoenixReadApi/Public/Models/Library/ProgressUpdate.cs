namespace PhoenixReadApi;

/// <summary>上传播放进度时使用的参数。</summary>
public sealed class ProgressUpdate
{
    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>所属剧集 ID。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>从 1 开始的分集序号；未知时为 0。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>当前播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>视频总时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>累计播放时长（毫秒）；未提供时客户端使用当前播放位置。</summary>
    public long AccumulatedPlayTimeMs { get; init; }

    /// <summary>最近观看时间（Unix 毫秒）；默认使用当前时间。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }

    /// <summary>是否把这条记录标记为删除。</summary>
    public bool UseSoftDelete { get; init; }
}
