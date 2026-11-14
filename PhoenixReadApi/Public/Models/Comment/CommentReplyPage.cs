namespace PhoenixReadApi;

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
