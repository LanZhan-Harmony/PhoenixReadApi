namespace PhoenixReadApi;

/// <summary>独立短视频元数据及详情（适用于红果圈短视频、视频搜索及合集子视频）。</summary>
public sealed class ShortVideo
{
    /// <summary>短视频唯一标识（Video ID / Series ID）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>用于解析播放直链的视频 ID（通常与 <see cref="Id"/> 一致）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>短视频标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>短视频简介或描述。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>封面图片直链。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>视频时长（秒）。</summary>
    public int DurationSeconds { get; init; }

    /// <summary>发布者信息。</summary>
    public User Author { get; init; } = new();

    /// <summary>参演演员与主创人员署名关系。</summary>
    public IReadOnlyList<DramaCredit> Credits { get; init; } = [];

    /// <summary>发布时间戳（Unix 秒数）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>获赞总数。</summary>
    public long LikeCount { get; init; }

    /// <summary>评论总数。</summary>
    public long CommentCount { get; init; }

    /// <summary>收藏总数。</summary>
    public long CollectCount { get; init; }

    /// <summary>播放总数。</summary>
    public long ViewCount { get; init; }

    /// <summary>当前登录账号是否已点赞。</summary>
    public bool IsLiked { get; init; }

    /// <summary>当前登录账号是否已收藏。</summary>
    public bool IsCollected { get; init; }

    /// <summary>关联的短剧列表卡片。</summary>
    public IReadOnlyList<Drama> RelatedDramas { get; init; } = [];
}
