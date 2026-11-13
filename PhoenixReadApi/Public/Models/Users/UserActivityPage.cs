namespace PhoenixReadApi;

/// <summary>账号主页中的一页动态记录。</summary>
public sealed class UserActivityPage
{
    /// <summary>本页动态。</summary>
    public IReadOnlyList<UserActivity> Items { get; init; } = [];

    /// <summary>下一页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数量。</summary>
    public long Total { get; init; }
}
