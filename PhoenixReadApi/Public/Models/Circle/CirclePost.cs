namespace PhoenixReadApi;

/// <summary>红果圈图文详情。</summary>
public sealed class CirclePost
{
    /// <summary>图文唯一标识（Post ID）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>纯文本内容。</summary>
    public string PureText { get; init; } = string.Empty;

    /// <summary>图集图片列表。</summary>
    public IReadOnlyList<CircleImage> Images { get; init; } = [];

    /// <summary>话题与标签列表（例如 ["#何聪睿", "#许梦圆"]）。</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>发布者信息。</summary>
    public User Author { get; init; } = new();

    /// <summary>发布时间戳（Unix 秒数）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>获赞总数。</summary>
    public long LikeCount { get; init; }

    /// <summary>评论总数。</summary>
    public long CommentCount { get; init; }

    /// <summary>收藏总数。</summary>
    public long CollectCount { get; init; }

    /// <summary>当前登录账号是否已点赞。</summary>
    public bool IsLiked { get; init; }

    /// <summary>当前登录账号是否已收藏。</summary>
    public bool IsCollected { get; init; }

    /// <summary>关联的短剧列表卡片。</summary>
    public IReadOnlyList<Drama> RelatedDramas { get; init; } = [];
}
