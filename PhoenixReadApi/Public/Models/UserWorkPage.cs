namespace PhoenixReadApi;

/// <summary>用户或演职人员主页作品分页结果。</summary>
public sealed class UserWorkPage
{
    /// <summary>作品短剧列表。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>下一页分页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多作品。</summary>
    public bool HasMore { get; init; }

    /// <summary>该分类作品总数。</summary>
    public long Total { get; init; }
}
