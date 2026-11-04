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

/// <summary>演员排行榜条目。</summary>
public sealed class ActorLeaderboardItem
{
    /// <summary>榜单名次（从 1 开始）。</summary>
    public int Position { get; init; }

    /// <summary>用户 ID。</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>演员 ID。</summary>
    public string ActorId { get; init; } = string.Empty;

    /// <summary>演员姓名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>演员头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>人气热度文案（如“700万人气”）。</summary>
    public string Popularity { get; init; } = string.Empty;

    /// <summary>代表作品。</summary>
    public string RepresentativeWork { get; init; } = string.Empty;

    /// <summary>副标题文案标签（如“10部作品”、“双周追剧200万人”）。</summary>
    public IReadOnlyList<string> SubTitles { get; init; } = [];

    /// <summary>排名变化类型（0: 维持不变, 1: 上升, 2: 下降）。</summary>
    public int RankChangeType { get; init; }

    /// <summary>排名变化数值（上升或下降的名次数）。</summary>
    public int RankChangeValue { get; init; }
}

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
