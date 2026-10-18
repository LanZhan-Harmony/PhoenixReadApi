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
}

/// <summary>评论回复分页结果。</summary>
public sealed class CommentReplyPage
{
    /// <summary>当前页回复。</summary>
    public IReadOnlyList<CommentReply> Items { get; init; } = [];

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }

    /// <summary>下一页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}
