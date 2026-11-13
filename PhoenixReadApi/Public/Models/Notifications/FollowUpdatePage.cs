namespace PhoenixReadApi;

/// <summary>关注追更短剧列表分页结果。</summary>
public sealed class FollowUpdatePage
{
    /// <summary>当前页追更短剧列表。</summary>
    public IReadOnlyList<FollowUpdateItem> Items { get; init; } = [];

    /// <summary>下一页分页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多追更短剧。</summary>
    public bool HasMore { get; init; }
}
