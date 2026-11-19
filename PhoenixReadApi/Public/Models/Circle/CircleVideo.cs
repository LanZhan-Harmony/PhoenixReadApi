namespace PhoenixReadApi;

/// <summary>红果圈短视频详情。</summary>
public sealed class CircleVideo
{
    /// <summary>视频唯一标识（VideoId / SeriesId）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>关联的短剧唯一 ID（若该视频关联了某部正片短剧）。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>视频描述或简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>时长（秒）。</summary>
    public long DurationSeconds { get; init; }

    /// <summary>播放量。</summary>
    public long PlayCount { get; init; }

    /// <summary>获赞量。</summary>
    public long LikeCount { get; init; }

    /// <summary>关注数/粉丝互动数。</summary>
    public long FollowerCount { get; init; }

    /// <summary>创建时间戳（Unix 秒数）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>发布者信息。</summary>
    public User Author { get; init; } = new();

    /// <summary>关联的演职人员阵容。</summary>
    public IReadOnlyList<DramaCredit> Celebrities { get; init; } = [];

    /// <summary>关联的原著/正片短剧信息。</summary>
    public Drama? RelatedDrama { get; init; }
}
