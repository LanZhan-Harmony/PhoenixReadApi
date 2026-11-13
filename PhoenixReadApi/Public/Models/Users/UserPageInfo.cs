namespace PhoenixReadApi;

/// <summary>账号主页某一内容分页的服务端状态。</summary>
public sealed class UserPageInfo
{
    /// <summary>下一页游标；没有游标时为空。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>基于偏移量的下一页起点。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数量。</summary>
    public long Total { get; init; }
}
