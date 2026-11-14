namespace PhoenixReadApi;

/// <summary>一页剧评或分集评论。</summary>
public sealed class CommentPage
{
    /// <summary>所查询的目标对象 ID（短剧 ID 或分集视频 ID）。</summary>
    public string TargetId { get; init; } = string.Empty;

    /// <summary>评论列表。</summary>
    public IReadOnlyList<Comment> Items { get; init; } = [];

    /// <summary>下一页分页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多评论。</summary>
    public bool HasMore { get; init; }

    /// <summary>该目标下的评论总数。</summary>
    public long Total { get; init; }
}
