namespace PhoenixReadApi;

/// <summary>收藏的图文帖子分页结果。</summary>
public sealed class PostFavoritePage
{
    /// <summary>当前页收藏的图文帖子列表。</summary>
    public IReadOnlyList<Post> Items { get; init; } = [];

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}
