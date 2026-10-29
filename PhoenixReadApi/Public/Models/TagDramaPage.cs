namespace PhoenixReadApi;

/// <summary>按题材标签分页获取的短剧列表页面。</summary>
public sealed record TagDramaPage
{
    /// <summary>当前页短剧列表。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有更多数据。</summary>
    public bool HasMore { get; init; }

    /// <summary>会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;
}
