namespace PhoenixReadApi;

/// <summary>短剧多维矩阵筛选返回的一页短剧列表。</summary>
public sealed class ShortDramaFilterPage
{
    /// <summary>当前页短剧条目。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>下一页分页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有更多短剧。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端本次筛选会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;
}
