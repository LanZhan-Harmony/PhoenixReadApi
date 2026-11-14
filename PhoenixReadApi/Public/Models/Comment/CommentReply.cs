namespace PhoenixReadApi;

/// <summary>评论下的一条回复。</summary>
public sealed class CommentReply
{
    /// <summary>回复 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>所属评论 ID。</summary>
    public string CommentId { get; init; } = string.Empty;

    /// <summary>回复目标用户 ID。</summary>
    public string ReplyToUserId { get; init; } = string.Empty;

    /// <summary>回复文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>回复作者。</summary>
    public User Author { get; init; } = new();

    /// <summary>创建时间（Unix 秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>获赞数。</summary>
    public long LikeCount { get; init; }

    /// <summary>回复附带的图片列表；若无附图则为空列表。</summary>
    public IReadOnlyList<CommentImage> Images { get; init; } = [];
}
