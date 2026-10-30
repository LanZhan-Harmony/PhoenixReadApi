namespace PhoenixReadApi;

/// <summary>评论或剧评的排序方式。</summary>
public enum CommentSortOrder
{
    /// <summary>按点赞和热度排序（默认）。</summary>
    Hot = 1,

    /// <summary>按发布时间最新排序。</summary>
    Latest = 0,
}
