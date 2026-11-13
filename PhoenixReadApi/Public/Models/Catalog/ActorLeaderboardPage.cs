namespace PhoenixReadApi;

/// <summary>短剧演员排行榜分页数据。</summary>
public sealed class ActorLeaderboardPage
{
    /// <summary>上榜演员列表。</summary>
    public IReadOnlyList<ActorLeaderboardItem> Items { get; init; } = [];

    /// <summary>是否还有更多数据。</summary>
    public bool HasMore { get; init; }

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>当前榜单会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;
}
