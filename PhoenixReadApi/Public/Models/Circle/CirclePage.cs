namespace PhoenixReadApi;

/// <summary>红果圈分页数据。</summary>
public sealed class CirclePage
{
    /// <summary>红果圈条目集合。</summary>
    public IReadOnlyList<CircleItem> Items { get; init; } = [];

    /// <summary>下一页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多数据。</summary>
    public bool HasMore { get; init; }

    /// <summary>总条目数。</summary>
    public long Total { get; init; }
}
