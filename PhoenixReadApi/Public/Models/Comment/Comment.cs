namespace PhoenixReadApi;

/// <summary>一条剧评或分集评论。</summary>
public sealed class Comment
{
    /// <summary>评论 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>评论正文内容。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>发表评论的用户信息。</summary>
    public User Author { get; init; } = new();

    /// <summary>发表时间戳（Unix 秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>对应正片视频内的毫秒时间点（若为分集弹幕/片段评论）。</summary>
    public long OffsetMs { get; init; }

    /// <summary>获赞数量。</summary>
    public long LikeCount { get; init; }

    /// <summary>该评论下的回复总数。</summary>
    public long ReplyCount { get; init; }

    /// <summary>评论中包含的图片或表情列表。</summary>
    public IReadOnlyList<CommentImage> Images { get; init; } = [];
}
