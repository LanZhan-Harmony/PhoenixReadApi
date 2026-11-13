namespace PhoenixReadApi;

/// <summary>一页剧评或分集评论。</summary>
public sealed class CommentPage
{
    /// <summary>本页评论所属的剧集或分集 ID。</summary>
    public string TargetId { get; init; } = string.Empty;

    /// <summary>评论列表。</summary>
    public IReadOnlyList<DramaComment> Items { get; init; } = [];

    /// <summary>下一页游标；没有下一页时为空。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端返回的评论总数。</summary>
    public long Total { get; init; }
}
