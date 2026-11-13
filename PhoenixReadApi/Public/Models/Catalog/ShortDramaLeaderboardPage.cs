namespace PhoenixReadApi;

/// <summary>短剧排行榜分页数据。</summary>
public sealed class ShortDramaLeaderboardPage
{
    /// <summary>上榜短剧列表。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>榜单标题或内容分类。</summary>
    public string BoardTitle { get; init; } = string.Empty;

    /// <summary>是否还有更多数据。</summary>
    public bool HasMore { get; init; }

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>当前榜单会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;
}
