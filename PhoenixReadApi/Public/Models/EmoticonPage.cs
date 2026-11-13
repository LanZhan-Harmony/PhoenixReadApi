namespace PhoenixReadApi;

/// <summary>表情包列表分页结果。</summary>
public sealed class EmoticonPage
{
    /// <summary>当前页包含的表情包图片列表。</summary>
    public IReadOnlyList<CommentImage> Items { get; init; } = [];

    /// <summary>下一页偏移量游标。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有更多表情包。</summary>
    public bool HasMore { get; init; }
}
